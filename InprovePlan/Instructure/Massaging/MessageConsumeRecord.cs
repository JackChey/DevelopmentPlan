using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Massaging;

using InprovePlan.Domain.BaseEntities;
using System;

/// <summary>
/// 消息消费记录实体 - 用于追踪消息队列的消费状态、重试情况及错误信息
/// </summary>
public class MessageConsumeRecord : AppAuditEntity // 假设存在仅包含Id和时间的基类，若无请移除继承并手动添加Id/CreatedAt/UpdatedAt
{
    /// <summary>
    /// 消息唯一标识
    /// 对应 MQ 中的 MessageId，用于去重和追踪
    /// </summary>
    public Guid MessageId { get; set; } 

    /// <summary>
    /// 消费者名称
    /// 标识处理该消息的具体服务或类名，如 "OrderService"
    /// </summary>
    public string ConsumerName { get; set; } = string.Empty;

    /// <summary>
    /// 业务数据ID
    /// 关联的具体业务实体ID，如订单ID、入库单ID等
    /// </summary>
    public long BusinessId { get; set; }

    /// <summary>
    /// 业务类型
    /// 标识业务领域，如 "Order", "StockIn", "StockOut"
    /// </summary>
    public string BusinessType { get; set; } = string.Empty;

    /// <summary>
    /// 消费状态
    /// Processing: 处理中, Success: 成功, Failed: 失败, Unknown: 未知
    /// </summary>
    public MessageConsumeStatus Status { get; set; } = MessageConsumeStatus.Unknown;

    /// <summary>
    /// 重试次数
    /// 记录当前消息已重试的次数
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// 错误信息
    /// 记录最后一次失败的异常消息或堆栈摘要
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 链路追踪ID
    /// 用于分布式链路追踪，关联日志系统
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// 获取或设置处理开始的时间戳。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该字段记录了业务逻辑或后台任务正式开始执行的具体时刻。
    /// 使用 <see cref="DateTimeOffset"/> 而非 <see cref="DateTime"/> 是为了保留创建该时间戳时的时区偏移量，
    /// 确保在分布式环境或多时区部署下，时间点的绝对唯一性和可追溯性。
    /// </para>
    /// <para>
    /// 若值为 <c>null</c>，表示处理尚未开始或状态未初始化。
    /// </para>
    /// </remarks>
    public DateTimeOffset? ProcessingStartedAt { get; set; }

    /// <summary>
    /// 获取或设置处理完成的时间戳。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该字段记录了业务逻辑或后台任务成功结束（或最终终止）的具体时刻。
    /// 结合 <see cref="ProcessingStartedAt"/>可用于计算处理耗时（Duration）。
    /// </para>
    /// <para>
    /// 使用 <see cref="DateTimeOffset"/> 确保即使服务器位于不同时区，
    /// 也能准确还原事件发生的全球统一时间点。
    /// </para>
    /// <para>
    /// 若值为 <c>null</c>，表示处理尚未完成、正在进行中或已失败但未记录结束时间。
    /// </para>
    /// </remarks>
    public DateTimeOffset? CompletedAt { get; set; }

}

/// <summary>
/// 消息消费状态枚举
/// </summary>
public enum MessageConsumeStatus
{
    /// <summary>
    /// 未知/初始状态
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 处理中
    /// </summary>
    Processing = 1,

    /// <summary>
    /// 成功
    /// </summary>
    Success = 2,

    /// <summary>
    /// 失败
    /// </summary>
    Failed = 3
}

