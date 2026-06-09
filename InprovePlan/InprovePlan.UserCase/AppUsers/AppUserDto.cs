using InprovePlan.Domain.Entities;

namespace InprovePlan.UserCase.AppUsers;

/// <summary>
/// 用户 DTO。
///
/// 用于返回给前端或应用层调用方。
///
/// 注意：
/// 1. DTO 不应该包含 PasswordHash。
/// 2. DTO 不应该暴露删除标记、删除时间等内部管理字段。
/// 3. DTO 只返回调用方真正需要展示或使用的数据。
/// </summary>
/// <summary>
/// 用户 DTO。
///
/// 用于返回给前端或应用层调用方。
///
/// 注意：
/// 1. DTO 不应该包含 PasswordHash。
/// 2. DTO 不应该暴露删除标记、删除时间等内部管理字段。
/// 3. DTO 只返回调用方真正需要展示或使用的数据。
/// </summary>
public sealed record AppUserDto(
    /// <summary>
    /// 用户 ID。
    ///
    /// 当前项目中 AppUser 继承 AppAuditEntity，
    /// AppAuditEntity 继承 BaseEntity<long>，
    /// 因此这里使用 long。
    /// </summary>
    long Id,

    /// <summary>
    /// 用户名。
    /// </summary>
    string UserName,

    /// <summary>
    /// 用户邮箱。
    /// </summary>
    string Email,

    /// <summary>
    /// 用户手机号。
    /// </summary>
    string PhoneNumber,

    /// <summary>
    /// 用户性别。
    /// </summary>
    AppUserSex Sex,

    /// <summary>
    /// 用户状态。
    /// </summary>
    AppUserStatus UserStatus);
