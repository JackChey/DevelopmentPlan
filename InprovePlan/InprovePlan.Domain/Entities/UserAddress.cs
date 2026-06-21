using InprovePlan.Domain.BaseEntities;

namespace InprovePlan.Domain.Entities
{
    /// <summary>
    /// 商品状态
    /// </summary>
    public enum UserAddressStatus
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
    public class UserAddress : AppAuditWithUserEntity
    {
        /// <summary>
        /// 地址名称
        /// </summary>
        public string AddressTypeName { get; set; } = string.Empty;

        /// <summary>
        /// 地址状态
        /// </summary>
        public UserAddressStatus AddressStatus { get; set;}
    }
}
