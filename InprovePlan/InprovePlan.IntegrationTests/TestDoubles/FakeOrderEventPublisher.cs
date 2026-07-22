using InprovePlan.ShareKernel.Contracts;
using InprovePlan.ShareKernel.Messaging;

namespace InprovePlan.IntegrationTests.TestDoubles;

/// <summary>
/// 伪造的订单事件发布器实现，用于测试或开发环境。
/// 实现了 IOrderEventPublisher 接口，不实际发送消息到消息队列，而是将发布的事件存储在内存列表中，以便后续验证。
/// </summary>
internal sealed class FakeOrderEventPublisher : IOrderEventPublisher
{
    /// <summary>
    /// 内部存储已发布事件的列表。
    /// 用于记录所有通过 PublishOrderStatusChangedAsync 方法发布的事件。
    /// </summary>
    private readonly List<OrderStatusChangedEvent> _events = [];

    /// <summary>
    /// 获取已发布事件的只读列表。
    /// 允许外部测试代码检查已发布的事件内容，但防止直接修改内部集合。
    /// </summary>
    public IReadOnlyList<OrderStatusChangedEvent> Events => _events;

    /// <summary>
    /// 发布订单状态变更事件。
    /// 在伪造实现中，仅将事件添加到内部列表，并立即返回已完成的任务，模拟异步操作但不执行实际的 network I/O。
    /// </summary>
    /// <param name="event">要发布的订单状态变更事件对象。</param>
    /// <param name="cancellationToken">取消令牌，在此实现中未被使用，因为操作是同步且瞬时的。</param>
    /// <returns>一个表示操作已完成的任务。</returns>
    public Task PublishOrderStatusChangedAsync(
        OrderStatusChangedEvent @event,
        CancellationToken cancellationToken = default)
    {
        // 将事件添加到内部列表中以供后续断言或验证
        _events.Add(@event);

        // 返回已完成的任务，符合异步方法签名要求，但无需真正异步执行
        return Task.CompletedTask;
    }
}

