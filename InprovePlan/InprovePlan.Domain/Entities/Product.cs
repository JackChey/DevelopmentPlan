using InprovePlan.Domain.BaseEntities;

namespace InprovePlan.Domain.Entities
{
    /// <summary>
    /// 商品状态
    /// </summary>
    public enum AppProductStatus
    {
        /// <summary>
        /// 新增
        /// </summary>
        Addition = 0,

        /// <summary>
        /// 启用
        /// </summary>
        Enable = 1,

        /// <summary>
        /// 作废
        /// </summary>
        Void = 2,

        /// <summary>
        /// 售罄
        /// </summary>
        SoldOut = 3,
    }

    /// <summary>
    /// 商品实体类
    /// </summary>
    public class Product:AppAuditWithUserEntity
    {
        /// <summary>
        /// 商品编号
        /// </summary>
        public string ProductCode { get; set; } = string.Empty;

        /// <summary>
        /// 商品名称
        /// </summary>
        public string ProductName { get; set;} = string.Empty;

        /// <summary>
        /// 商品描述
        /// </summary>
        public string ProductDescription { get; set; } = string.Empty;

        /// <summary>
        /// 商品分类
        /// </summary>
        public int ProductTypeId { get; set;} 

        /// <summary>
        /// 商品状态
        /// </summary>
        public AppProductStatus ProductStatus { get; set;}

        /// <summary>
        /// 商品单价
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 货币类型
        /// </summary>
        public string Currency { get; set; } = string.Empty;
    }
}
