# 06-01 Idempotency-Key 机制实现

## 目录

1. [Git 日志和 diff](#1-git-日志和-diff)
2. [幂等表结构或迁移文件](#2-幂等表结构或迁移文件)
3. [接入的接口说明](#3-接入的接口说明)
4. [幂等 Key 作用域说明](#4-幂等-key-作用域说明)
5. [请求 Hash 策略说明](#5-请求-hash-策略说明)
6. [并发控制说明](#6-并发控制说明)
7. [Postman / curl 验证记录](#7-postman--curl-验证记录)
8. [数据库记录截图或 SQL 查询结果](#8-数据库记录截图或-sql-查询结果)
9. [重复请求、参数冲突、并发请求的测试结果](#9-重复请求参数冲突并发请求的测试结果)

## 1. Git 日志和 diff

待补充。

## 2. 幂等表结构或迁移文件

本次实现新增了 `IdempotencyRecords` 表，用于保存一次幂等请求的处理状态、请求指纹和首次成功响应结果。

相关迁移文件：

```text
Instructure/Migrations/20260630114249_UpdateEntityConfigurations_AddIdempotencyRecords.cs
Instructure/Migrations/20260701124032_UpdateEntityConfigurations_updateidempotencyrecords_01.cs
Instructure/Migrations/20260701124545_UpdateEntityConfigurations_updateidempotencyrecords_02.cs
```

核心实体：

```text
Instructure/Idempotency/IdempotencyRecord.cs
```

核心配置：

```text
Instructure/Configurations/Entities/IdempotencyRecordConfiguration.cs
```

### 表结构

表名：

```text
IdempotencyRecords
```

主要字段：

| 字段                   | 类型             | 说明                                                 |
| ---------------------- | ---------------- | ---------------------------------------------------- |
| `Id`                   | `bigint`         | 主键，使用业务侧 ID 生成器生成                       |
| `Key`                  | `varchar(128)`   | 客户端传入的幂等键                                   |
| `RequestHash`          | `varchar(128)`   | 请求内容 Hash，用于识别同一个 Key 是否被用于不同参数 |
| `UserId`               | `bigint`         | 当前请求所属用户                                     |
| `Method`               | `varchar(124)`   | 当前操作来源或操作类型，目前为 MediatR 管道语义值    |
| `Path`                 | `varchar(1024)`  | 当前业务请求类型名或操作路径                         |
| `Status`               | `int`            | 幂等记录状态                                         |
| `ResponseStatusCode`   | `int?`           | 首次请求成功时的响应状态码，当前保留字段             |
| `ResponseBody`         | `longtext`       | 首次请求成功时的完整统一响应 JSON                    |
| `ErrorMessage`         | `varchar(1024)?` | 失败原因                                             |
| `CompletedAt`          | `datetime(6)?`   | 请求完成时间                                         |
| `ExpiresAt`            | `datetime(6)`    | 幂等记录过期时间，用于后续清理                       |
| `RowVersion`           | `timestamp(6)`   | 并发控制字段                                         |
| `CreatedAt`            | `datetime(6)`    | 创建时间                                             |
| `LastModifiedAt`       | `datetime(6)?`   | 最后修改时间                                         |
| `CreatedByUserId`      | `bigint?`        | 创建人                                               |
| `LastModifiedByUserId` | `bigint?`        | 最后修改人                                           |

### 状态枚举

状态枚举定义在：

```text
Instructure/Idempotency/IdempotencyRecordStatus.cs
```

当前核心状态：

| 状态         | 说明                                     |
| ------------ | ---------------------------------------- |
| `Processing` | 请求已登记，业务正在执行                 |
| `Succeeded`  | 请求已成功完成，重复请求可以返回首次响应 |
| `Failed`     | 请求执行失败，后续重复请求按保守策略处理 |

### 索引设计

唯一索引：

```text
UX_IdempotencyRecords_UserId_Key
```

当前实际索引字段：

```text
UserId + Key
```

作用：

```text
同一个用户下，同一个 Idempotency-Key 只能创建一条幂等记录。
```

这条唯一索引是服务端幂等的最终防线。即使 Redis 缓存失效、分布式锁失效或多个请求同时进入数据库，数据库层面也只能有一个请求成功创建 `Processing` 记录。

辅助索引：

```text
IX_IdempotencyRecords_ExpiresAt
IX_IdempotencyRecords_Status_CreatedAt
IX_IdempotencyRecords_CreatedByUserId
IX_IdempotencyRecords_LastModifiedByUserId
```

用途：

- `ExpiresAt` 用于后续定时清理过期幂等记录。
- `Status + CreatedAt` 用于排查或补偿长时间卡在 `Processing` 的请求。
- 用户审计索引用于追踪创建人和修改人。

### 原子插入

幂等记录创建不使用普通 `Add + SaveChanges` 处理唯一冲突，而是通过专用仓储执行数据库原子插入：

```text
Instructure/Repositories/IIdempotencyRecordRepository.cs
Instructure/Repositories/IdempotencyRecordRepository.cs
```

核心方法：

```csharp
Task<bool> TryCreateProcessingAsync(
    IdempotencyRecord record,
    CancellationToken cancellationToken = default);
```

该方法用于尝试插入 `Processing` 状态记录：

- 返回 `true`：插入成功，表示当前请求是首次请求。
- 返回 `false`：记录已存在，表示当前请求是重复请求，需要查询已有记录后决定返回 `Cached`、`Processing` 或 `Conflict`。

## 3. 接入的接口说明

本次幂等机制当前接入在订单创建接口：

```http
POST /api/AppOrder/CreateWithIdempotency
```

控制器位置：

```text
InprovePlan/Controllers/AppOrderController.cs
```

请求模型：

```csharp
public sealed record CreateAppOrderWithIdempotencyRequest(
    long ProductId,
    decimal Quantity,
    long AddressId,
    string IdempotencyKey);
```

当前 `IdempotencyKey` 由请求体传入：

```json
{
  "productId": 818715775590469,
  "quantity": 10,
  "addressId": 818715764654149,
  "idempotencyKey": "ABCDEFG123456"
}
```

控制器将请求转换为 MediatR Command：

```csharp
new CreateAppOrderWithIdempotencyCommand(
    request.ProductId,
    request.Quantity,
    request.AddressId,
    request.IdempotencyKey)
```

业务命令位置：

```text
InprovePlan.UserCase/AppOrders/Commands/CreateAppOrderWithIdempotencyCommand.cs
```

命令定义：

```csharp
public sealed record CreateAppOrderWithIdempotencyCommand(
    long ProductId,
    decimal Quantity,
    long AddressId,
    string IdempotencyKey
) : ICommand<Result<AppOrderDto>>, IIdempotentRequest;
```

接入方式：

- 需要幂等控制的 MediatR 请求实现 `IIdempotentRequest`。
- `IdempotencyBehavior<TRequest, TResponse>` 在 MediatR 管道中识别该接口。
- 普通请求不实现 `IIdempotentRequest`，不会进入幂等逻辑。

MediatR 管道位置：

```text
InprovePlan.UserCase/Behaviors/IdempotencyBehavior.cs
```

幂等服务位置：

```text
InprovePlan.UserCase/Idempotency/IdempotencyService.cs
```

执行流程：

```text
HTTP Request
  -> AppOrderController.CreateWithIdempotency
  -> CreateAppOrderWithIdempotencyCommand
  -> IdempotencyBehavior
  -> IdempotencyService.BeginAsync
  -> Handler
  -> IdempotencyService.CompleteAsync / FailAsync
  -> HTTP Response
```

## 4. 幂等 Key 作用域说明

当前幂等 Key 的作用域为：

```text
UserId + IdempotencyKey
```

也就是说，同一个用户下，相同的 `IdempotencyKey` 被视为同一次业务意图。

当前数据库唯一索引也按该规则约束：

```text
UNIQUE (UserId, Key)
```

### 为什么需要包含 UserId

不能只使用客户端传入的 `IdempotencyKey` 作为全局唯一判断依据。

例如两个用户都提交：

```text
IdempotencyKey = ABCDEFG123456
```

如果只按 `Key` 判断，可能导致用户之间互相影响。加入 `UserId` 后，相同 Key 在不同用户之间互不干扰。

### 当前缓存 Key

Redis 幂等记录缓存 Key：

```text
idempotencyservice:record:{IdempotencyKey}:{UserId}
```

分布式锁 Key：

```text
idempotencyservice:key:{IdempotencyKey}:{UserId}
```

两类 Key 职责不同：

- `record` 用于缓存幂等记录状态和响应。
- `key` 用于短时间并发互斥。

### 租户说明

当前实现未引入 `TenantId` 字段，实际作用域是 `UserId + Key`。如果后续系统进入多租户场景，建议扩展为：

```text
TenantId + UserId + IdempotencyKey
```

并同步调整数据库唯一索引与 Redis Key 构造规则。

## 5. 请求 Hash 策略说明

请求 Hash 用于解决一个关键问题：

```text
同一个 Idempotency-Key 不能被用于不同请求参数。
```

例如第一次请求：

```json
{
  "productId": 1,
  "quantity": 1,
  "addressId": 100,
  "idempotencyKey": "KEY-001"
}
```

第二次请求：

```json
{
  "productId": 2,
  "quantity": 99,
  "addressId": 100,
  "idempotencyKey": "KEY-001"
}
```

两次请求使用了相同 Key，但业务参数不同，应返回冲突，不允许继续执行业务。

### 当前 Hash 生成位置

Hash 生成入口：

```text
InprovePlan.UserCase/Behaviors/IdempotencyBehavior.cs
```

Hash 计算实现：

```text
InprovePlan.UserCase/Idempotency/RequestHashProvider.cs
```

### 当前参与 Hash 的内容

`IdempotencyBehavior` 会将当前 MediatR Command 序列化为 JSON：

```csharp
var body = JsonSerializer.Serialize(request, request.GetType());
```

然后构造 `RequestHashSource`：

```csharp
new RequestHashSource
{
    Method = "MediatR_ComputeRequestHash",
    Path = typeof(TRequest).FullName ?? typeof(TRequest).Name,
    QueryString = string.Empty,
    UserId = currentUser.Id ?? 0,
    Body = body
}
```

`RequestHashProvider` 将以下内容组合并标准化：

| 字段          | 策略                            |
| ------------- | ------------------------------- |
| `Method`      | 转为大写                        |
| `Path`        | 转为小写                        |
| `QueryString` | 当前为空字符串                  |
| `UserId`      | 当前登录用户 ID                 |
| `Body`        | MediatR Command 序列化后的 JSON |

最终使用 SHA256 生成十六进制字符串：

```csharp
SHA256.HashData(Encoding.UTF8.GetBytes(json))
```

### 当前 Hash 语义

当前策略可以保证：

- 同一用户、同一 Command 类型、同一业务参数会得到相同 Hash。
- 同一用户、同一 Key 但业务参数不同会得到不同 Hash。
- 重复请求命中已有记录时，如果 `RequestHash` 不一致，返回 `Conflict`。

### 注意事项

当前 Command 序列化会包含 `IdempotencyKey` 字段。由于数据库唯一作用域已经包含 `Key`，这不会影响“同一个 Key 不允许不同参数”的判断。但从语义上看，后续可以优化为在生成业务 Body Hash 时排除 `IdempotencyKey`，让 Hash 更纯粹地表达业务参数。

建议的长期策略：

```text
RequestHash = SHA256(UserId + OperationName + BusinessPayloadWithoutIdempotencyKey)
```

## 6. 并发控制说明

本次实现采用三层并发控制：

```text
Redis 缓存预查
  -> Redis 分布式锁
  -> 数据库唯一索引 + 原子插入
```

### 第一层：Redis 幂等记录缓存

请求进入 `BeginAsync` 后，首先查询 Redis 中的幂等记录缓存：

```text
idempotencyservice:record:{IdempotencyKey}:{UserId}
```

如果缓存命中：

| 状态                 | 处理                                      |
| -------------------- | ----------------------------------------- |
| `Succeeded`          | 反序列化 `ResponseBody`，直接返回缓存响应 |
| `Processing`         | 返回处理中，拒绝重复执行                  |
| `Failed`             | 当前保守处理为处理中                      |
| `RequestHash` 不一致 | 返回冲突                                  |

该层用于减少数据库访问，并让重复请求尽快返回。

### 第二层：Redis 分布式锁

当 Redis 记录缓存未命中时，服务尝试获取分布式锁：

```text
idempotencyservice:key:{IdempotencyKey}:{UserId}
```

作用：

```text
防止多个相同 Key 的请求在瞬时并发下同时穿透到数据库。
```

如果锁获取失败，说明同一个幂等请求正在被其他线程或实例处理，直接返回 `Processing`。

注意：

```text
分布式锁只是并发优化，不是最终正确性依据。
```

因为锁可能释放、过期、Redis 可能短暂不可用，所以仍然必须依赖数据库唯一约束兜底。

### 第三层：数据库唯一索引和原子插入

在获取分布式锁后，服务调用：

```csharp
IIdempotencyRecordRepository.TryCreateProcessingAsync(...)
```

该方法通过数据库原子插入创建 `Processing` 记录。

结果：

- 插入成功：说明当前请求获得处理权，返回 `Started`，继续执行业务 Handler。
- 插入失败：说明记录已存在，查询已有记录并根据状态返回。

数据库唯一索引：

```text
UNIQUE (UserId, Key)
```

这是整个幂等机制的最终防线。

### 成功完成流程

Handler 返回成功结果后，`IdempotencyBehavior` 调用：

```csharp
IIdempotencyService.CompleteAsync(...)
```

服务更新数据库记录：

```text
Status = Succeeded
ResponseBody = 完整 Result<T> JSON
CompletedAt = UtcNow
```

然后写入 Redis 记录缓存：

```text
idempotencyservice:record:{IdempotencyKey}:{UserId}
```

后续重复请求可直接返回首次响应。

### 失败流程

Handler 返回失败或抛出异常时，服务调用：

```csharp
IIdempotencyService.FailAsync(...)
```

服务更新数据库记录：

```text
Status = Failed
ErrorMessage = 错误信息
CompletedAt = UtcNow
```

并删除 Redis 记录缓存，使后续请求回源数据库判断。

当前失败策略偏保守：`Failed` 状态不会自动重新执行业务，以避免业务已经部分成功时重复执行。

## 7. Postman / curl 验证记录

### 首次下单

#### 日志记录

{"Event":"http.request.started","Http":{"Method":"POST","Route":"/api/AppOrder/CreateWithIdempotency","StatusCode":200,"DurationMs":null,"ClientIp":"::1"},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-07-04T10:52:06.9037808+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-18232","TraceId":"338f5ce78d828d90cbf8c9f0e3d652dd","SpanId":"0dce8feb3e17f1c4"}
{"Event":"http.request.completed","Http":{"Method":"POST","Route":"/api/AppOrder/CreateWithIdempotency","StatusCode":200,"DurationMs":1249.5879,"ClientIp":"::1"},"Auth":{"IsAuthenticated":true,"UserId":"820052270506053","UserName":"user100001","AuthScheme":"AuthenticationTypes.Federation","Roles":["user"]},"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-07-04T10:52:08.1536297+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-18232","TraceId":"338f5ce78d828d90cbf8c9f0e3d652dd","SpanId":"0dce8feb3e17f1c4"}

#### 幂等键记录

{"key":"ABCDEFG123456","requestHash":"F6045C461ADA1C05E957A34638EC0820D1239A55AD9D6A5971C0D674A491024B","userId":820052270506053,"method":"MediatR_Check_Idempotency","path":"InprovePlan.UserCase.AppOrders.Commands.CreateAppOrderWithIdempotencyCommand","status":2,"responseStatusCode":null,"responseBody":"{\u0022Status\u0022:0,\u0022Message\u0022:null,\u0022Errors\u0022:[],\u0022IsSuccess\u0022:true,\u0022Value\u0022:{\u0022Id\u0022:822948371435589,\u0022OrderNo\u0022:\u0022O20260703120531618545525\u0022,\u0022ProductId\u0022:818715775590469,\u0022ProductName\u0022:\u0022Rustic-Soft-00001\u0022,\u0022ProductCode\u0022:\u0022SP818715775590469\u0022,\u0022Currency\u0022:\u0022CNY\u0022,\u0022UnitPrice\u0022:8981.11,\u0022Quantity\u0022:10,\u0022TotalAmount\u0022:89811.10,\u0022UserId\u0022:820052270506053,\u0022OccurredTime\u0022:\u00222026-07-03T12:05:31.6212444\u002B00:00\u0022,\u0022OrderStatus\u0022:0,\u0022Cancelled\u0022:false,\u0022AddressId\u0022:818715764654149}}","errorMessage":null,"completedAt":"2026-07-03T12:06:28.1181145+00:00","expiresAt":"2026-07-04T12:05:07.60136+00:00","rowVersion":"CN7ZPpH7mT4=","createdByUserId":null,"lastModifiedByUserId":820052270506053,"createdAt":"2026-07-03T12:05:09.550658+00:00","lastModifiedAt":"2026-07-03T12:06:29.2975807+00:00","id":822948277506117}

#### 响应结果

statuscode:200
responsebody:
{
"success": true,
"data": {
"id": 822948371435589,
"orderNo": "O20260703120531618545525",
"productId": 818715775590469,
"productName": "Rustic-Soft-00001",
"productCode": "SP818715775590469",
"currency": "CNY",
"unitPrice": 8981.11,
"quantity": 10,
"totalAmount": 89811.1,
"userId": 820052270506053,
"occurredTime": "2026-07-03T12:05:31.6212444+00:00",
"orderStatus": 0,
"cancelled": false,
"addressId": 818715764654149
},
"error": null,
"traceId": "0HNMPEROOQHV7:00000004"
}

### 重复下单

#### 日志记录

{"Event":"http.request.started","Http":{"Method":"POST","Route":"/api/AppOrder/CreateWithIdempotency","StatusCode":200,"DurationMs":null,"ClientIp":"::1"},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-07-04T10:55:01.2146885+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-18232","TraceId":"7ed2fad7b3cefed6cad9a6c99bff7483","SpanId":"2c46a794c6ddde18"}
{"Event":"http.request.completed","Http":{"Method":"POST","Route":"/api/AppOrder/CreateWithIdempotency","StatusCode":200,"DurationMs":1057.9547,"ClientIp":"::1"},"Auth":{"IsAuthenticated":true,"UserId":"820052270506053","UserName":"user100001","AuthScheme":"AuthenticationTypes.Federation","Roles":["user"]},"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-07-04T10:55:02.2726774+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-18232","TraceId":"7ed2fad7b3cefed6cad9a6c99bff7483","SpanId":"2c46a794c6ddde18"}

#### 幂等键记录

本次没有新增幂等键

#### 响应结果

statuscode:200
responsebody:
{
"success": true,
"data": {
"id": 822948371435589,
"orderNo": "O20260703120531618545525",
"productId": 818715775590469,
"productName": "Rustic-Soft-00001",
"productCode": "SP818715775590469",
"currency": "CNY",
"unitPrice": 8981.11,
"quantity": 10,
"totalAmount": 89811.1,
"userId": 820052270506053,
"occurredTime": "2026-07-03T12:05:31.6212444+00:00",
"orderStatus": 0,
"cancelled": false,
"addressId": 818715764654149
},
"error": null,
"traceId": "0HNMPEROOQHV7:00000004"
}

#### 对比可看出重复下单会返回已成功结果

### 用户使用同样幂等键但是请求参数不同进行下单

#### 日志记录

{"Event":"http.request.started","Http":{"Method":"POST","Route":"/api/AppOrder/CreateWithIdempotency","StatusCode":200,"DurationMs":null,"ClientIp":"::1"},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-07-04T13:17:24.762963+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-18516","TraceId":"2e93ba166c02f46dee0cc7ad72005123","SpanId":"afcc3132fe5dbb47"}
{"Event":"exception.handled","Http":{"Method":"POST","Route":"/api/AppOrder/CreateWithIdempotency","StatusCode":400,"DurationMs":null,"ClientIp":"::1"},"Error":{"Code":"request_conflict","Type":"Instructure.Exceptions.IdempotencyException","Message":"The same idempotency key was used with a different request payload.","Stack":" at InprovePlan.UserCase.Behaviors.IdempotencyBehavior\u00602.Handle(TRequest request, RequestHandlerDelegate\u00601 next, CancellationToken cancellationToken) in D:\\Learn\\dotnet-90days-bootcamp\\InprovePlan\\InprovePlan.UserCase\\Behaviors\\IdempotencyBehavior.cs:line 151\r\n at InprovePlan.UserCase.Behaviors.ValidationBehavior\u00602.Handle(TRequest request, RequestHandlerDelegate\u00601 next, CancellationToken cancellationToken) in D:\\Learn\\dotnet-90days-bootcamp\\InprovePlan\\InprovePlan.UserCase\\Behaviors\\ValidationBehavior.cs:line 41\r\n at InprovePlan.UserCase.Behaviors.AuthorizationBehavior\u00602.Handle(TRequest request, RequestHandlerDelegate\u00601 next, CancellationToken cancellationToken) in D:\\Learn\\dotnet-90days-bootcamp\\InprovePlan\\InprovePlan.UserCase\\Behaviors\\AuthorizationBehavior.cs:line 94\r\n at InprovePlan.Controllers.AppOrderController.CreateWithIdempotency(CreateAppOrderWithIdempotencyRequest request, CancellationToken cancellationToken) in D:\\Learn\\dotnet-90days-bootcamp\\InprovePlan\\InprovePlan\\Controllers\\AppOrderController.cs:line 83\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.TaskOfIActionResultExecutor.Execute(ActionContext actionContext, IActionResultTypeMapper mapper, ObjectMethodExecutor executor, Object controller, Object[] arguments)\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.\u003CInvokeActionMethodAsync\u003Eg**Awaited|12_0(ControllerActionInvoker invoker, ValueTask\u00601 actionResultValueTask)\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.\u003CInvokeNextActionFilterAsync\u003Eg**Awaited|10_0(ControllerActionInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Rethrow(ActionExecutedContextSealed context)\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Next(State\u0026 next, Scope\u0026 scope, Object\u0026 state, Boolean\u0026 isCompleted)\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.\u003CInvokeInnerFilterAsync\u003Eg**Awaited|13_0(ControllerActionInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.\u003CInvokeFilterPipelineAsync\u003Eg**Awaited|20_0(ResourceInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.\u003CInvokeAsync\u003Eg**Awaited|17_0(ResourceInvoker invoker, Task task, IDisposable scope)\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.\u003CInvokeAsync\u003Eg**Awaited|17_0(ResourceInvoker invoker, Task task, IDisposable scope)\r\n at Microsoft.AspNetCore.Authorization.AuthorizationMiddleware.Invoke(HttpContext context)\r\n at InprovePlan.Middlewares.RequestLifecycleMiddleware.Invoke(HttpContext ctx) in D:\\Learn\\dotnet-90days-bootcamp\\InprovePlan\\InprovePlan\\Middlewares\\RequestLifecycleMiddleware.cs:line 57\r\n at InprovePlan.Middlewares.AuthLogContextMiddleware.Invoke(HttpContext context) in D:\\Learn\\dotnet-90days-bootcamp\\InprovePlan\\InprovePlan\\Middlewares\\AuthLogContextMiddleware.cs:line 32\r\n at Microsoft.AspNetCore.Authentication.AuthenticationMiddleware.Invoke(HttpContext context)\r\n at Swashbuckle.AspNetCore.SwaggerUI.SwaggerUIMiddleware.Invoke(HttpContext httpContext)\r\n at Swashbuckle.AspNetCore.Swagger.SwaggerMiddleware.Invoke(HttpContext httpContext, ISwaggerProvider swaggerProvider)\r\n at Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddlewareImpl.\u003CInvoke\u003Eg\_\_Awaited|10_0(ExceptionHandlerMiddlewareImpl middleware, HttpContext context, Task task)","SourceContext":"InprovePlan.Exceptions.GlobalExceptionHandler"},"Tags":null,"OccurrenceTime":"2026-07-04T13:17:27.4755729+08:00","Level":"Warning","Msg":"Handled_Exception","Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-18516","TraceId":"2e93ba166c02f46dee0cc7ad72005123","SpanId":"afcc3132fe5dbb47"}

#### 幂等键记录

本次没有新增幂等键

#### 响应结果

statuscode:400
responsebody:
{
"success": false,
"data": null,
"error": {
"code": "request_conflict",
"message": "The same idempotency key was used with a different request payload.",
"details": null
},
"traceId": "00-2e93ba166c02f46dee0cc7ad72005123-afcc3132fe5dbb47-00"
}

待补充。

## 8. 数据库记录截图或 SQL 查询结果

### 查询幂等键记录

#### 查询SQL

SELECT \* FROM `idempotencyrecords` WHERE UserId = 820052270506053 ;

#### 查询结果

{"key":"ABCDEFG123456","requestHash":"F6045C461ADA1C05E957A34638EC0820D1239A55AD9D6A5971C0D674A491024B","userId":820052270506053,"method":"MediatR_Check_Idempotency","path":"InprovePlan.UserCase.AppOrders.Commands.CreateAppOrderWithIdempotencyCommand","status":2,"responseStatusCode":0,"responseBody":"{\u0022Status\u0022:0,\u0022Message\u0022:null,\u0022Errors\u0022:[],\u0022IsSuccess\u0022:true,\u0022Value\u0022:{\u0022Id\u0022:823201545318469,\u0022OrderNo\u0022:\u0022O20260704051541530720191\u0022,\u0022ProductId\u0022:818715775590469,\u0022ProductName\u0022:\u0022Rustic-Soft-00001\u0022,\u0022ProductCode\u0022:\u0022SP818715775590469\u0022,\u0022Currency\u0022:\u0022CNY\u0022,\u0022UnitPrice\u0022:8981.11,\u0022Quantity\u0022:10,\u0022TotalAmount\u0022:89811.10,\u0022UserId\u0022:820052270506053,\u0022OccurredTime\u0022:\u00222026-07-04T05:15:41.5324851\u002B00:00\u0022,\u0022OrderStatus\u0022:0,\u0022Cancelled\u0022:false,\u0022AddressId\u0022:818715764654149}}","errorMessage":null,"completedAt":"2026-07-04T05:15:42.5172822+00:00","expiresAt":"2026-07-05T05:15:40.283321+00:00","rowVersion":"CN7ZzlnRgWQ=","createdByUserId":null,"lastModifiedByUserId":820052270506053,"createdAt":"2026-07-04T05:15:40.289704+00:00","lastModifiedAt":"2026-07-04T05:15:42.543387+00:00","id":823201539141701}

### 查询订单结果

#### 查询SQL

SELECT \* FROM `app_orders` WHERE UserId = 820052270506053 ;

#### 查询结果

[
{
"id": 818715780059205,
"orderNo": "SO0000000001",
"productId": 818715775701068,
"productName": "Rustic-Frozen-01484",
"productCode": "SP818715775701068",
"currency": "CNY",
"unitPrice": 9927.18,
"quantity": 2.038,
"totalAmount": 20231.59284,
"userId": 820052270506053,
"occurredTime": "2026-03-18T20:11:38.163216+00:00",
"orderStatus": 3,
"cancelled": false,
"addressId": 818715765375056
},
{
"id": 818715780063301,
"orderNo": "SO0000000002",
"productId": 818715775623288,
"productName": "Unbranded-Steel-00407",
"productCode": "SP818715775623288",
"currency": "CNY",
"unitPrice": 1760.44,
"quantity": 1.537,
"totalAmount": 2705.79628,
"userId": 820052270506053,
"occurredTime": "2025-03-10T03:46:29.034262+00:00",
"orderStatus": 2,
"cancelled": false,
"addressId": 818715765043303
},
{
"id": 823201545318469,
"orderNo": "O20260704051541530720191",
"productId": 818715775590469,
"productName": "Rustic-Soft-00001",
"productCode": "SP818715775590469",
"currency": "CNY",
"unitPrice": 8981.11,
"quantity": 10,
"totalAmount": 89811.1,
"userId": 820052270506053,
"occurredTime": "2026-07-04T05:15:41.532485+00:00",
"orderStatus": 0,
"cancelled": false,
"addressId": 818715764654149
}
]

## 9. 重复请求、参数冲突、并发请求的测试结果

### 重复请求测试结果

重复请求返回之前已存在的结果

### 参数冲突测试结果

返回操作失败,statuscode 为 400,并提示:The same idempotency key was used with a different request payload

### 并发请求的测试结果

暂无
