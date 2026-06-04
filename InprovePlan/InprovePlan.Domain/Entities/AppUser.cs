using InprovePlan.Domain.BaseEntities;

namespace InprovePlan.Domain.Entities
{
    /// <summary>
    /// 用户状态
    /// </summary>
    public enum AppUserStatus
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
        /// 冻结
        /// </summary>
        Frozen = 3,
    }

    /// <summary>
    /// 性别
    /// </summary>
    public enum AppUserSex
    {
        /// <summary>
        /// 男性
        /// </summary>
        Male = 0,

        /// <summary>
        /// 女性
        /// </summary>
        Female = 1,

        /// <summary>
        /// 保密
        /// </summary>
        Secret = 2,
    }

    /// <summary>
    /// 系统用户实体类
    /// </summary>
    public class AppUser: AppAuditEntity
    {
        /// <summary>
        /// 密码哈希值
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 性别
        /// </summary>
        public AppUserSex Sex {  get; set; }

        /// <summary>
        /// 用户电话
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// 用户邮箱
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 用户状态
        /// </summary>
        public AppUserStatus UserStatus { get; set; }

        /// <summary>
        /// 是否删除
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// 删除时间
        /// </summary>
        public DateTimeOffset? DeletedAt { get; set; }
    }
}
