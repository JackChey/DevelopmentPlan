namespace InprovePlan.Domain.Entities;

using InprovePlan.Domain.BaseEntities;

/// <summary>
/// 出库明细实体 - 记录每一笔销售、领用或损耗出库的业务流水
/// </summary>
public class StockOutRecord: AppAuditWithUserEntity
{
    /// <summary>
    /// 关联商品ID
    /// 外键指向 Product.Id
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 商品名称快照
    /// 冗余数据，确保历史出库记录的商品名称不被后续修改影响
    /// </summary>
    public string ProductNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 商品编码快照
    /// 冗余数据，便于快速识别
    /// </summary>
    public string ProductCodeSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 出库数量
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性: 关联的商品
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// 获取或设置源消息的唯一标识符。
    /// 通常用于关联原始的消息队列消息、事件总线事件或外部系统的回调ID，以便进行链路追踪或去重处理。
    /// </summary>
    public Guid? SourceMessageId { get; set; }

    /// <summary>
    /// 获取或设置源业务实体的唯一标识符。
    /// 指向触发当前操作或与之关联的核心业务对象ID（如订单ID、用户ID、交易流水号等），用于建立业务数据间的关联。
    /// </summary>
    public long? SourceBusinessId { get; set; }

    /// <summary>
    /// 获取或设置源操作的动作类型或名称。
    /// 用于描述触发当前记录的具体行为（例如："Create"、"Update"、"PaymentSuccess"、"Cancel" 等），便于后续的状态机判断或审计日志分析。
    /// </summary>
    public string? SourceAction { get; set; }

}

