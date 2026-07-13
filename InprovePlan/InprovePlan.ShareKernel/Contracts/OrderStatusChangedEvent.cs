using InprovePlan.Domain.Entities;

namespace InprovePlan.ShareKernel.Contracts;

/// <summary>
/// 订单状态变更事件。
/// 
/// 注意：
/// 1. 这是已经发生的事实，不是命令。
/// 2. 命名使用 Event，而不是 Command。
/// 3. 消费者不能通过这个事件决定订单是否应该变更，订单状态已经在订单服务中改完了。
/// </summary>
public sealed record OrderStatusChangedEvent
{
    /// <summary>
    /// 消息唯一 ID。
    /// 用于消费者幂等处理。
    /// </summary>
    public required Guid MessageId { get; init; }

    /// <summary>
    /// 订单 ID。
    /// </summary>
    public required long OrderId { get; init; }

    /// <summary>
    /// 变更前状态。
    /// </summary>
    public required AppOrderStatus FromStatus { get; init; }

    /// <summary>
    /// 变更后状态。
    /// </summary>
    public required AppOrderStatus ToStatus { get; init; }

    /// <summary>
    /// 状态变更原因，例如用户取消、支付成功、超时关闭。
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// 操作人 ID。
    /// 系统自动任务可为空。
    /// </summary>
    public long? OperatorId { get; init; }

    /// <summary>
    /// 事件发生时间。
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// 链路追踪 ID。
    /// 用于串联 API 日志、Outbox、MQ、消费者日志。
    /// </summary>
    public string? TraceId { get; init; }
}
