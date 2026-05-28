## 模拟3类故障并定位（可复现演练）

## !!! 以下故障演练均在本地演练

### 演练信息

- 演练日期：2026-05-28
- 分支/提交：
- 演练人：朱广贵
- 环境：`Development`
- 目标：验证故障可观测、可定位、可恢复

---

## 故障1：Prometheus 依赖不可达

### 1. 注入方式

- 方式：修改 `PrometheusSettings:Port` 为错误端口（示例：`9090 -> 19090`）/ 停止 Prometheus 服务
- 注入时间：2026-05-28 14:16

### 2. 触发步骤

1. 调用 `GET /api/Metrics/P50`
2. 调用 `GET /api/Metrics/P90`
3. 调用 `GET /api/Metrics/P95`

### 3. 预期结果

- 接口返回失败，不出现“0ms 假数据”
- 返回体包含失败原因（如 `prometheus_http_error:*` / `exception:*`）
- 日志中可见对应错误事件与 `TraceId`

### 4. 实际结果

- 实际响应：
  StatusCode:500,
  Response:{
  "success": false,
  "data": null,
  "error": {
  "code": "internal_error",
  "message": "Request failed",
  "details": [
  "查询失败: exception:HttpRequestException"
  ]
  },
  "traceId": "0HNLSG42KE2A4:00000001"
  }
- 实际日志：
  {"Event":"","Http":{"Method":"GET","Route":"/api/Metrics/P50","StatusCode":500,"DurationMs":4232.1751,"ClientIp":null},"Error":{"Code":"code","Type":null,"Message":null,"Stack":null},"Tags":null,"OccurrenceTime":"2026-05-28T14:22:05.2541551+08:00","Level":"Error","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-11228","TraceId":"d1675bd5397ab76505b9865adb6b4e0a","SpanId":"df49479cee67d2cd"}

- 是否符合预期：否

### 5. 定位过程

- 首个异常信号：接口响应状态为 500,对应响应结果中包含异常信息:HttpRequestException
- 定位路径（文件/模块）：/Service/Prometheus/PrometheusQueryService.cs
- 根因结论：Prometheus 连接地址不正确导致查询失败

### 6. 修复与恢复

- 恢复动作：修改Prometheus的连接配置
- 恢复验证：修改配置完成后重新调用接口,观测接口响应结果是否正常
- 恢复耗时：5m

### 7. 证据

- 请求/响应截图：

修改后接口响应结果:
StatusCode:200,
Response:
{
"success": true,
"data": null,
"error": null,
"traceId": "0HNLSGCO647FQ:00000001"
}

- 日志截图：
  请求日志:
  {"Event":"http.request.completed","Http":{"Method":"GET","Route":"/api/Metrics/P50","StatusCode":200,"DurationMs":3797.3969,"ClientIp":"::1"},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-05-28T14:36:29.9672532+08:00","Level":"Information","Msg":"http.request.completed","Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-22712","TraceId":"561c112e27727ae11a89fab9def74942","SpanId":"11eb5706dd6c0792"}
  无异常日志

- 配置前后截图：
  修改前配置:
  "PrometheusSettings": {
  "IP": "localhost",
  "Port": 9091,
  "TimeoutSeconds": 5,
  "HttpDurationBucketMetric": "http_request_duration_seconds_bucket",
  "FailFastOnMetricMissing": false
  },

修改后配置:
"PrometheusSettings": {
"IP": "localhost",
"Port": 9090,
"TimeoutSeconds": 5,
"HttpDurationBucketMetric": "http_request_duration_seconds_bucket",
"FailFastOnMetricMissing": false
},

- git 证据（commit/hash）：作为测试链路不推送到git

### 备注:

修复后日志样例:
{"Event":"http.request.completed","Http":{"Method":"GET","Route":"/api/Metrics/P50","StatusCode":500,"DurationMs":4676.3321,"ClientIp":"::1"},"Auth":{"IsAuthenticated":true,"UserId":"100001","UserName":"Jack","AuthScheme":"AuthenticationTypes.Federation","Roles":["Admin"]},"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-05-28T20:22:32.914581+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-17532","TraceId":"87e9672fb550c27c662e3a66efa7c701","SpanId":"7ac33a6be28dc5ca"}

---

## 故障2：鉴权失败链路（401/403）

### 1. 注入方式

- 401：不带 Token 访问受保护接口
- 403：使用权限不足 Token 访问高权限接口（若当前未实现403，可标注计划）

### 2. 触发步骤

1. 请求受保护接口（无 Token）
2. 请求受保护接口（低权限 Token）

### 3. 预期结果

- 401/403 统一响应格式一致
- 日志事件正确（如 `auth.access.unauthorized` / `auth.access.forbidden`）
- `TraceId` 与请求可关联

### 4. 实际结果

- 实际响应：
  StatusCode:401,Response:{
  "success": false,
  "data": null,
  "error": {
  "code": "unauthorized",
  "message": "Unauthorized",
  "details": null
  },
  "traceId": "0HNLSGH16382S:00000001"
  }
- 实际日志：
  {"Event":"","Http":{"Method":"GET","Route":"/api/User","StatusCode":401,"DurationMs":37.3748,"ClientIp":null},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-05-28T14:48:18.6096338+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-18088","TraceId":"2abce21e354383eb9ce753da40c204aa","SpanId":"ae01ca4835243905"}
- 是否符合预期：是

### 5. 定位过程

- 首个异常信号：接口响应状态401,表示接口未授权
- 定位路径:/Controllers/UserController.cs
- 根因结论：接口要求授权而访问时未携带正确的JWT

### 6. 修复与恢复

- 修复动作：通过登录接口获取JWT,在Swagger页面使用JWT登录
- 恢复验证：按照之前的参数重新访问接口,观测响应结果是否符合预期
- 恢复耗时：3m

### 7. 证据

- 请求/响应截图：
  StatusCode:200,
  Response:
  {
  "success": true,
  "data": null,
  "error": null,
  "traceId": "0HNLSGH16382T:00000004"
  }
- 日志截图：
  {"Event":"http.request.completed","Http":{"Method":"GET","Route":"/api/User","StatusCode":200,"DurationMs":2.0502,"ClientIp":"::1"},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-05-28T14:54:52.9942439+08:00","Level":"Information","Msg":"http.request.completed","Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-18088","TraceId":"97db4b1b6880082860aa06c7c9fcd247","SpanId":"47c8aa5ec248b06a"}
- Swagger 显示 401/403 截图：
- git 证据（commit/hash）：

### 备注:

修复后日志样例:
{"Event":"http.request.completed","Http":{"Method":"GET","Route":"/api/User","StatusCode":401,"DurationMs":206.5711,"ClientIp":"::1"},"Auth":{"IsAuthenticated":false,"UserId":null,"UserName":null,"AuthScheme":null,"Roles":[]},"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-05-28T20:24:22.4377987+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-17532","TraceId":"aa99cd455d900c84b88fef9f3f5715c7","SpanId":"849e5a578f35313a"}

---

## 故障3：应用未处理异常（500）

### 1. 注入方式

- 构造可控异常（测试参数触发 throw / 临时测试接口抛异常）

### 2. 触发步骤

1. 调用触发异常接口
2. 观察响应与异常日志分流文件

### 3. 预期结果

- 返回统一错误响应（500）
- 异常日志包含：`Error.Code/Type/Message/Stack`
- 请求日志与异常日志可通过 `TraceId/SpanId` 串联

### 4. 实际结果

- 实际响应：
  StatusCode:500,
  Response:
  {
  "success": false,
  "data": null,
  "error": {
  "code": "client_closed_request",
  "message": "Client closed request",
  "details": null
  },
  "traceId": "00-a4c1ce479e0c6a6069661a705e86cf2b-522479eb1b9bc53f-00"
  }
- 实际日志：
  {"Event":"http.request.failed","Http":{"Method":"GET","Route":"/api/User","StatusCode":500,"DurationMs":null,"ClientIp":"::1"},"Error":{"Code":"client_closed_request","Type":"System.OperationCanceledException","Message":"\u7528\u6237\u540D\u975E\u6CD5","Stack":" at InprovePlan.Controllers.UserController.Update(Int32 userId, String userName) in D:\\Learn\\dotnet-90days-bootcamp\\InprovePlan\\InprovePlan\\Controllers\\UserController.cs:line 98\r\n at lambda_method4(Closure, Object, Object[])\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.SyncActionResultExecutor.Execute(ActionContext actionContext, IActionResultTypeMapper mapper, ObjectMethodExecutor executor, Object controller, Object[] arguments)\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.InvokeActionMethodAsync()\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Next(State\u0026 next, Scope\u0026 scope, Object\u0026 state, Boolean\u0026 isCompleted)\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.InvokeNextActionFilterAsync()\r\n--- End of stack trace from previous location ---\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Rethrow(ActionExecutedContextSealed context)\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Next(State\u0026 next, Scope\u0026 scope, Object\u0026 state, Boolean\u0026 isCompleted)\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.InvokeInnerFilterAsync()\r\n--- End of stack trace from previous location ---\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.\u003CInvokeFilterPipelineAsync\u003Eg**Awaited|20_0(ResourceInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.\u003CInvokeAsync\u003Eg**Awaited|17_0(ResourceInvoker invoker, Task task, IDisposable scope)\r\n at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.\u003CInvokeAsync\u003Eg**Awaited|17_0(ResourceInvoker invoker, Task task, IDisposable scope)\r\n at Microsoft.AspNetCore.Authorization.AuthorizationMiddleware.Invoke(HttpContext context)\r\n at InprovePlan.Middlewares.RequestLifecycleMiddleware.Invoke(HttpContext ctx) in D:\\Learn\\dotnet-90days-bootcamp\\InprovePlan\\InprovePlan\\Middlewares\\RequestLifecycleMiddleware.cs:line 153\r\n at InprovePlan.Middlewares.AuthLogContextMiddleware.Invoke(HttpContext context) in D:\\Learn\\dotnet-90days-bootcamp\\InprovePlan\\InprovePlan\\Middlewares\\AuthLogContextMiddleware.cs:line 33\r\n at Microsoft.AspNetCore.Authentication.AuthenticationMiddleware.Invoke(HttpContext context)\r\n at Swashbuckle.AspNetCore.SwaggerUI.SwaggerUIMiddleware.Invoke(HttpContext httpContext)\r\n at Swashbuckle.AspNetCore.Swagger.SwaggerMiddleware.Invoke(HttpContext httpContext, ISwaggerProvider swaggerProvider)\r\n at Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddlewareImpl.\u003CInvoke\u003Eg**Awaited|10_0(ExceptionHandlerMiddlewareImpl middleware, HttpContext context, Task task)"},"Tags":null,"OccurrenceTime":"2026-05-28T19:54:38.5848963+08:00","Level":"Error","Msg":"Unhandled_Exception","Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-28116","TraceId":"a4c1ce479e0c6a6069661a705e86cf2b","SpanId":"522479eb1b9bc53f"}

- 是否符合预期：是

### 5. 定位过程

- 首个异常信号：接口抛出异常
- 定位路径:路由:/api/User --> 抛出异常 --> 全局异常处理:GlobalExceptionHandler --> 日志输出
- 根因结论：输入规定的测试数据抛出异常

### 6. 修复与恢复

- 修复动作：修改校验规则
- 恢复验证：修改后重新输入测试数据观测接口响应结果
- 恢复耗时：1m

### 7. 证据

- 请求/响应截图：
  StatusCode:200,
  Response:
  {
  "success": true,
  "data": null,
  "error": null,
  "traceId": "0HNLSM2EOVMI8:00000002"
  }

- 异常日志截图：无异常日志输出
- 分流文件截图（请求日志 vs 异常日志）：
  请求日志:
  {"Event":"http.request.completed","Http":{"Method":"GET","Route":"/api/User","StatusCode":200,"DurationMs":227.2472,"ClientIp":"::1"},"Auth":{"IsAuthenticated":true,"UserId":"100001","UserName":"Jack","AuthScheme":"AuthenticationTypes.Federation","Roles":["Admin"]},"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-05-28T20:00:58.6865342+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-17532","TraceId":"ae05b0bb690f61dc7ea6baf06e20f5ff","SpanId":"e20cfc28cf6d677c"}
  无异常日志

- git 证据（commit/hash）：

### 备注:

该错误码为演练注入码，非生产默认映射

---

## 统一复盘结论

### 1. 本次发现的问题清单（按严重级）

- P0：日志信息不一致
- P1：日志信息不完整
- P2：日志来源过多

### 2. 已完成修复

- [ ] 问题A
- [ ] 问题B
- [ ] 问题C

### 3. 待办（下一周）

- [ ] 补 401/403 自动化测试
- [ ] 补 P50/P95 查询稳定性测试
- [ ] 补健康检查真实依赖探测
- [ ] 403 暂未实现，留给后续故障演练

### 4. 关键指标

- 平均定位时间（MTTD）：10m
- 平均恢复时间（MTTR）：10m
- 是否满足目标：是
