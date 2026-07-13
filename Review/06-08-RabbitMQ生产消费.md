# 06-08 RabbitMQ 生产消费

## 1. 目标

### 1.1 文档目的

验证当前项目中 RabbitMQ 主链路是否具备生产消费的基础能力，包括：

- 订单 API 完成核心订单状态修改。
- 使用 MassTransit 发布 `OrderStatusChangedEvent`。
- 使用 EF Outbox 保证消息先落库后投递。
- RabbitMQ 将消息路由到消费者队列。
- `OrderStatusChangedConsumer` 完成订单取消后的库存退回处理。
- 消费者使用消费记录表和业务唯一索引保证幂等。

### 1.2 验证范围

本 Review 覆盖：

- MassTransit + RabbitMQ 配置。
- EF Outbox 配置。
- 订单状态变更事件契约。
- 订单状态变更消费者。
- 消费记录表设计。
- 库存退回业务幂等设计。

### 1.3 不验证范围

以下内容本次不做现场验证：

- RabbitMQ 管理台截图。
- 真实日志截图。
- Git 提交记录。
- 压测数据。
- 生产环境告警联动。

### 1.4 生产消费链路总览

```text
订单 API 修改订单状态
 -> 同事务调用 IPublishEndpoint.Publish
 -> EF Outbox 写入 OutboxMessage
 -> MassTransit 后台投递 RabbitMQ
 -> RabbitMQ exchange: order.status.changed
 -> queue: order-status-changed-queue
 -> OrderStatusChangedConsumer
 -> 消费去重
 -> 库存退回
 -> 消费记录 Success
 -> ACK
```

## 2. 代码位置

### 2.1 RabbitMQ 配置位置

```text
D:\Learn\dotnet-90days-bootcamp\InprovePlan\InprovePlan.UserCase\DependencyInjection.cs
```

方法：

```text
AddRabbitMq
```

### 2.2 MassTransit 注册位置

```csharp
services.AddMassTransit(x =>
{
    x.AddConsumer<OrderStatusChangedConsumer>();
    ...
});
```

### 2.3 Outbox 配置位置

```csharp
x.AddEntityFrameworkOutbox<AppDbContext>(o =>
{
    o.QueryDelay = TimeSpan.FromSeconds(1);
    o.UseMySql();
    o.UseBusOutbox();
});
```

### 2.4 生产者代码位置

事件发布接口和实现：

```text
IOrderEventPublisher
OrderEventPublisher
```

调用位置应位于订单状态修改成功后，例如：

```csharp
await eventPublisher.PublishOrderStatusChangedAsync(@event, cancellationToken);
```

### 2.5 消费者代码位置

```text
D:\Learn\dotnet-90days-bootcamp\InprovePlan\InprovePlan.UserCase\Customers\OrderStatusChangedConsumer.cs
```

### 2.6 事件契约位置

```text
D:\Learn\dotnet-90days-bootcamp\InprovePlan\InprovePlan.ShareKernel\Contracts\OrderStatusChangedEvent.cs
```

## 3. 核心设计

### 3.1 订单状态修改职责划分

订单核心状态由订单 API 或订单服务负责修改。RabbitMQ 不负责决定订单状态，只负责广播“订单状态已经变化”的事实。

### 3.2 为什么订单 API 负责核心状态修改

订单状态属于核心业务事实，应在订单服务本地事务中完成，保证客户端可以及时得到明确结果。

### 3.3 为什么 RabbitMQ 只负责异步通知

RabbitMQ 用于解耦附属动作，例如：

- 退库存。
- 退优惠券。
- 发送通知。
- 积分处理。
- 数据同步。

附属动作失败不应影响订单主状态。

### 3.4 Outbox 可靠消息设计

启用：

```csharp
o.UseBusOutbox();
```

含义：

```text
Publish 不直接发 RabbitMQ，而是先写 OutboxMessage。
当前 DbContext 事务提交成功后，MassTransit 后台服务再投递 RabbitMQ。
```

可避免：

```text
订单状态已修改，但服务宕机导致 MQ 消息未发送。
```

### 3.5 Consumer 消费设计

消费者：

```text
OrderStatusChangedConsumer
```

主要职责：

- 查询或创建 `MessageConsumeRecord`。
- 判断消息是否已经成功消费。
- 对 `Processing` 超时记录进行接管。
- 对 `Failed` 且未达重试上限的记录允许重试。
- 对取消订单事件创建库存退回流水。
- 将消费记录标记为 `Success`。

### 3.6 ACK 时机设计

MassTransit 默认在消费者方法正常结束后 ACK。当前消费者中，业务处理成功后才正常返回。

异常时重新抛出：

```csharp
throw;
```

从而触发 MassTransit Retry 或进入 error queue。

### 3.7 事务边界设计

当前消费者注入：

```text
AppDbContext
```

并使用：

```csharp
await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
...
await dbContext.SaveChangesAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

保证以下内容一起提交：

```text
库存退回流水 StockInRecord
消费记录 MessageConsumeRecord = Success
```

### 3.8 多实例部署下的消费行为

多实例下可能出现同一消息并发消费。当前依赖：

```text
MessageConsumeRecord: MessageId + ConsumerName 唯一索引
StockInRecord: SourceBusinessId + SourceAction 唯一索引
```

用于防止重复业务处理。

## 4. 生产消费完整流程

### 4.1 客户端请求订单状态修改

示例：

```http
POST /api/orders/{orderId}/cancel
Idempotency-Key: {client-generated-key}
Authorization: Bearer {token}
Content-Type: application/json
```

### 4.2 订单 API 修改订单状态

订单 API 完成：

- 鉴权。
- 参数校验。
- 幂等校验。
- 订单状态机校验。
- 修改订单状态。
- 写订单状态日志。

### 4.3 同事务写入 Outbox

订单状态修改后发布：

```csharp
await publishEndpoint.Publish(orderStatusChangedEvent, cancellationToken);
```

启用 Bus Outbox 后，此处写入 `OutboxMessage`。

### 4.4 Outbox 投递 RabbitMQ

MassTransit 后台投递服务扫描 Outbox 表，并发送到 RabbitMQ。

### 4.5 RabbitMQ 路由消息

当前配置：

```csharp
cfg.Message<OrderStatusChangedEvent>(m =>
{
    m.SetEntityName("order.status.changed");
});
```

消费队列：

```text
order-status-changed-queue
```

### 4.6 Consumer 接收消息

由 `OrderStatusChangedConsumer` 消费 `OrderStatusChangedEvent`。

### 4.7 Consumer 执行业务逻辑

当前实现针对：

```text
ToStatus = AppOrderStatus.Cancel
```

执行库存退回。

### 4.8 Consumer 成功 ACK

业务成功并提交事务后，消费者正常返回，MassTransit ACK。

## 5. 请求/消息示例

### 5.1 订单状态修改请求示例

```json
{
  "reason": "用户主动取消"
}
```

### 5.2 OrderStatusChangedEvent 示例

```json
{
  "messageId": "5c7f7e62-66a9-4d54-bd42-28c1d0b84c01",
  "orderId": 10001,
  "fromStatus": "PendingPayment",
  "toStatus": "Cancel",
  "reason": "用户主动取消",
  "operatorId": 123,
  "occurredAt": "2026-07-13T10:00:00+08:00",
  "traceId": "trace-20260713-0001"
}
```

### 5.3 RabbitMQ 消息 Payload 示例

MassTransit 实际发送时会包含 envelope 和 headers。业务 Payload 中至少应包含：

```text
MessageId
OrderId
FromStatus
ToStatus
OccurredAt
TraceId
```

### 5.4 Headers / TraceId 示例

```text
traceId: trace-20260713-0001
messageType: OrderStatusChangedEvent
consumer: OrderStatusChangedConsumer
```

## 6. 数据库记录截图或 SQL 结果

### 6.1 订单状态修改前 SQL

```sql
SELECT id, status, product_id, quantity
FROM app_orders
WHERE id = 10001;
```

### 6.2 订单状态修改后 SQL

```sql
SELECT id, status, last_modified_at
FROM app_orders
WHERE id = 10001;
```

### 6.3 OutboxMessage 写入结果

```sql
SELECT *
FROM OutboxMessage
ORDER BY SequenceNumber DESC
LIMIT 10;
```

待现场验证。

### 6.4 Outbox 投递后记录变化

待现场验证。RabbitMQ 正常时，Outbox 消息可能很快被投递并清理或标记完成。

### 6.5 MessageConsumeRecord 消费记录

```sql
SELECT *
FROM message_consume_records
WHERE message_id = '5c7f7e62-66a9-4d54-bd42-28c1d0b84c01'
  AND consumer_name = 'OrderStatusChangedConsumer';
```

### 6.6 StockInRecord 业务结果

```sql
SELECT *
FROM stock_in_records
WHERE source_business_id = 10001
  AND source_action = 'ReturnStockForOrderCancel';
```

## 7. RabbitMQ 队列截图或管理台截图

### 7.1 Exchange 截图

待补充。

### 7.2 Queue 截图

待补充。

### 7.3 Binding 截图

待补充。

### 7.4 消息投递前后队列变化

待补充。

### 7.5 Consumer 连接状态

待补充。

## 8. 日志证据

### 8.1 API 修改订单日志

{"Event":"http.request.started","Http":{"Method":"PUT","Route":"/api/AppOrder/823201545318469/status","StatusCode":200,"DurationMs":null,"ClientIp":"::1"},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-07-13T19:17:54.1663189+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-5856","TraceId":"08c8c42ce2bee6209909c62495797fbe","SpanId":"2d75599185358c0a"}
{"Event":"cache.remove","Http":{"Method":"","Route":"/api/AppOrder/823201545318469/status","StatusCode":0,"DurationMs":0,"ClientIp":null},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-07-13T19:18:04.5497568+08:00","Level":"Information","Msg":"Cache_Remove","Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-5856","TraceId":"08c8c42ce2bee6209909c62495797fbe","SpanId":"2d75599185358c0a"}
{"Event":"http.request.completed","Http":{"Method":"PUT","Route":"/api/AppOrder/823201545318469/status","StatusCode":200,"DurationMs":10483.0673,"ClientIp":"::1"},"Auth":{"IsAuthenticated":true,"UserId":"820052270506053","UserName":"user100001","AuthScheme":"AuthenticationTypes.Federation","Roles":["user"]},"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-07-13T19:18:04.6498054+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-5856","TraceId":"08c8c42ce2bee6209909c62495797fbe","SpanId":"2d75599185358c0a"}

### 8.2 Outbox 投递日志

#### Outbox 消息记录 -- 此时 RabbitMq 为断开状态

##### 查询 outboxmessage

```sql
SELECT * FROM `outboxmessage`;
```

查询结果:
{
"messageId": "e0160000-2307-f4ce-dff7-08dee0d06337",
"requestId": null,
"correlationId": null,
"conversationId": "e0160000-2307-f4ce-3a3e-08dee0d0633b",
"initiatorId": null,
"sourceAddress": "rabbitmq://localhost/order_vhost/ZGG_InprovePlan_bus_hymyyybdy94c79n1bdxqbwber1?temporary=true",
"destinationAddress": "rabbitmq://localhost/order_vhost/order.status.changed",
"responseAddress": null,
"faultAddress": null,
"messageType": [
"urn:message:InprovePlan.ShareKernel.Contracts:OrderStatusChangedEvent"
],
"message": {
"messageId": "7964d263-8707-4c31-8566-1507a01d2f18",
"orderId": 823201545318469,
"fromStatus": 9,
"toStatus": 2,
"reason": "取消订单",
"operatorId": 820052270506053,
"occurredAt": "2026-07-13T11:17:55.5274537+00:00",
"traceId": null
},
"expirationTime": null,
"sentTime": "2026-07-13T11:17:55.5614711Z",
"headers": {},
"host": {
"machineName": "ZGG",
"processName": "InprovePlan",
"processId": 5856,
"assembly": "InprovePlan",
"assemblyVersion": "1.0.0.0",
"frameworkVersion": "8.0.4",
"massTransitVersion": "8.2.2.0",
"operatingSystemVersion": "Microsoft Windows NT 10.0.22631.0"
}
}

##### 查询 outboxstate

```sql
SELECT * FROM `outboxstate`;
```

查询结果:

{
"outbox_id": "e0160000-2307-f4ce-ab1f-08dee0d0631e",
"lock_id": "00000000-0000-0000-0000-000000000000",
"created_at": "2026-07-13T19:18:04.432302",
"updated_at": "2026-07-13T11:17:55.587620",
"next_send_time": null,
"error_message": null
}

##### 查询 inboxstate

```sql
SELECT * FROM `inboxstate`;
```

查询结果: null

##### (OutBox)消息记录 -- 此时 RabbitMq 重新连接

再次执行上面的查询Sql,发现 outboxmessage , outboxstate , inboxstate 都不存在数据,此时 RabbitMq 成功连接后之前的滞留消息被成功发送
再次查看库存明细表 stock_in_records 发现对应库存已增加

### 8.3 RabbitMQ 发布日志

待补充。

### 8.4 Consumer 接收日志

当前消费者包含：

```csharp
logger.LogInformation("Event:{@event},TraceId={@traceId},Msg:{@msg}", ...);
```

{"Event":"consumer.start","Http":{"Method":"","Route":"","StatusCode":0,"DurationMs":0,"ClientIp":null},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-07-13T20:05:19.4042188+08:00","Level":"Information","Msg":"ConsumerName:OrderStatusChangedConsumer,MessageId:c53da404-a10e-46fa-a67a-11b8fb42ce88","Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-33392","TraceId":"","SpanId":""}

### 8.5 Consumer 成功处理日志

{"Event":"consumer.success","Http":{"Method":"","Route":"","StatusCode":0,"DurationMs":0,"ClientIp":null},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-07-13T20:05:19.5188468+08:00","Level":"Information","Msg":"ConsumerName:OrderStatusChangedConsumer,MessageId:c53da404-a10e-46fa-a67a-11b8fb42ce88","Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-33392","TraceId":"","SpanId":""}

### 8.6 TraceId 串联证据

待补充。

## 9. 风险与未完成项

### 9.1 RabbitMQ 不可用风险

RabbitMQ 不可用时，Outbox 可暂存待发送消息，但需要监控 Outbox 积压。

### 9.2 Outbox 积压风险

缺少 Outbox 积压告警。

### 9.3 Consumer 异常风险

当前消费者会将异常抛出交给 MassTransit Retry，但死信处理后台尚未实现。

### 9.4 事务边界风险

消费者已使用显式事务。生产前仍需验证所有仓储共享同一个 scoped `AppDbContext`。

### 9.5 运维后台未完成项

缺少：

- 死信归集。
- 死信详情查看。
- 人工重投。
- 人工标记已处理。
- 处理审计。

### 9.6 监控告警未完成项

缺少：

- RabbitMQ 队列堆积告警。
- error queue 告警。
- Outbox 积压告警。
- Failed 消费记录告警。

## 10. 结论

### 10.1 验证结果

从代码设计看，RabbitMQ 主链路已经具备联调和学习环境可用条件。

### 10.2 是否满足学习目标

满足。

### 10.3 是否满足生产条件

有条件满足。还需要补齐运维、告警、补偿和死信处理闭环。

### 10.4 最终结论

```text
生产有条件通过
```
