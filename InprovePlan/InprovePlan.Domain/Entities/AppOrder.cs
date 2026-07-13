using InprovePlan.Domain.BaseEntities;

namespace InprovePlan.Domain.Entities
{
    /// <summary>
    /// 订单状态
    /// </summary>
    public enum AppOrderStatus
    {
        /// <summary>
        /// 新增,待支付
        /// </summary>
        Addition = 0,

        /// <summary>
        /// 支付完成待发货
        /// </summary>
        Paid = 1,

        /// <summary>
        /// 发货完成
        /// </summary>
        Shipment = 2,

        /// <summary>
        /// 运输中
        /// </summary>
        Transporting = 3,

        /// <summary>
        /// 派送中
        /// </summary>
        Delivering = 4,

        /// <summary>
        /// 送达
        /// </summary>
        Delivered = 5,

        /// <summary>
        /// 已签收
        /// </summary>
        Received = 6,

        /// <summary>
        /// 拒收
        /// </summary>
        Refuse = 7,

        /// <summary>
        /// 退货
        /// </summary>
        Return = 8,

        /// <summary>
        /// 取消订单
        /// </summary>
        Cancel = 9,
    }

    /// <summary>
    /// 商品订单实体类
    /// </summary>
    public class AppOrder:AppAuditWithUserEntity
    {
        /// <summary>
        /// 订单编号
        /// </summary>
        public string OrderNo { get; set; } = string.Empty;

        /// <summary>
        /// 商品ID,关联 Product 的主键 Id
        /// </summary>
        public long ProductId { get; set; }

        /// <summary>
        /// 商品名称,冗余数据,避免商品名称改动而引发信息疑问
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// 商品编码快照
        /// </summary>
        public string ProductCode { get; set; } = string.Empty;

        /// <summary>
        /// 支付货币类型
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// 商品单价,冗余数据,避免商品金额改动而引发信息疑问
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 总金额
        /// </summary>
        public decimal TotalAmount { get; private set; }

        /// <summary>
        /// 商品下单数量
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// 下单人ID,关联 AppUser.Id
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// 下单时间
        /// </summary>
        public DateTimeOffset OccurredTime { get; set; }

        /// <summary>
        /// 订单状态
        /// </summary>
        public AppOrderStatus OrderStatus { get; set; }

        /// <summary>
        /// 收货地址ID,关联后续地址Id,地址实体留待后续完善
        /// </summary>
        public long AddressId { get; set; }

        /// <summary>
        /// 订单是否取消
        /// </summary>
        public bool Cancelled { get; set; }

        /// <summary>
        /// 导航属性,管理商品
        /// </summary>
        public Product Product { get; set; } = null!;

        /// <summary>
        /// 导航属性,管理下单人
        /// </summary>
        public AppUser User { get; set; } = null!;

        public void RecalculateTotalAmount()
        {
            TotalAmount = UnitPrice * Quantity;
        }
    }
}
