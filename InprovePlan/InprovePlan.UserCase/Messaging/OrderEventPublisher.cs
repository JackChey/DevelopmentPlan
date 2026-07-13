namespace InprovePlan.UserCase.Messaging;

using InprovePlan.ShareKernel.Contracts;
using InprovePlan.ShareKernel.Messaging;
using MassTransit;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 订单事件发布器实现类。
/// 负责将订单领域的领域事件（Domain Events）发布到消息总线中。
/// 使用 sealed 关键字防止被继承，确保行为的一致性。
/// </summary>
public sealed class OrderEventPublisher : IOrderEventPublisher
{
    /// <summary>
    /// MassTransit 的消息发布端点接口。
    /// 用于将消息发送到配置好的消息代理（如 RabbitMQ, Azure Service Bus等）。
    /// </summary>
    private readonly IPublishEndpoint _publishEndpoint;

    /// <summary>
    /// 构造函数，通过依赖注入获取消息发布端点。
    /// </summary>
    /// <param name="publishEndpoint">MassTransit 提供的发布端点实例</param>
    public OrderEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// 异步发布订单状态变更事件。
    /// </summary>
    /// <param name="event">包含订单状态变更详细信息的领域事件对象</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌，默认值为 default</param>
    /// <returns>表示异步发布操作的任务</returns>
    public Task PublishOrderStatusChangedAsync(
        OrderStatusChangedEvent @event,
        CancellationToken cancellationToken = default)
    {
        // 【核心逻辑说明】
        // 调用 MassTransit 的 Publish 方法发布事件。
        //
        // 【关于事务一致性 (EF Outbox 模式)】
        // 如果系统中配置了 Entity Framework Outbox (发件箱模式)，此调用具有以下行为：
        //
        // 1. 消息持久化：
        //    在当前数据库事务范围内，消息不会直接发送到消息中间件，而是先序列化并保存到本地的 "Outbox" 表中。
        //    这确保了消息的保存与业务数据（如订单状态更新）在同一个原子事务中。
        //
        // 2. 异步发送：
        //    当数据库事务成功提交后，MassTransit 的后台服务会轮询 Outbox 表，
        //    将未发送的消息可靠地传输到消息代理（如 RabbitMQ）。
        //
        // 3. 解决分布式一致性问题：
        //    这种机制避免了“本地数据库更新成功，但消息发送失败”导致的数据不一致问题。
        //    即使应用进程在事务提交后、消息发送前崩溃，后台服务重启后仍会从 Outbox 表中重试发送，保证消息至少投递一次。

        return _publishEndpoint.Publish(@event, cancellationToken);
    }
}

