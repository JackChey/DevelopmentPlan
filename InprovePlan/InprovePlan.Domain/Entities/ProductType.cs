using InprovePlan.Domain.BaseEntities;

namespace InprovePlan.Domain.Entities
{
    /// <summary>
    /// 商品状态
    /// </summary>
    public enum ProductTypeStatus
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
    }

    /// <summary>
    /// 商品实体类
    /// </summary>
    public class ProductType : AppAuditWithUserEntity
    {
        /// <summary>
        /// 分类名称
        /// </summary>
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// 分类描述
        /// </summary>
        public string TypeDescription { get; set; } = string.Empty;

        /// <summary>
        /// 分类状态
        /// </summary>
        public ProductTypeStatus TypeStatus { get; set;}
    }
}
