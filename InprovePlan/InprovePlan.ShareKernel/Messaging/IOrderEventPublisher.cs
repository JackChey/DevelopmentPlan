namespace InprovePlan.ShareKernel.Messaging;

using InprovePlan.ShareKernel.Contracts;

public interface IOrderEventPublisher
{
    /// <summary>
    /// 发布订单状态变更事件。
    /// 
    /// 注意：
    /// 如果启用了 MassTransit EF Outbox，
    /// 这里调用 Publish 不会立刻发送 RabbitMQ，
    /// 而是先写入 Outbox 表。
    /// 当前数据库事务提交后，MassTransit 后台服务再可靠投递。
    /// </summary>
    Task PublishOrderStatusChangedAsync(
        OrderStatusChangedEvent @event,
        CancellationToken cancellationToken = default);
}
