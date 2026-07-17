using InprovePlan.Domain.Entities;

namespace InprovePlan.UnitTests.TestData;

/// <summary>
/// 应用用户测试数据常量类
/// </summary>
public static class AppUserTestData
{
    /// <summary>
    /// 有效的用户ID
    /// </summary>
    public const long ValidUserId = 1;

    /// <summary>
    /// 无效的用户ID（通常用于测试边界条件或异常场景）
    /// </summary>
    public const long InvalidUserId = 0;

    /// <summary>
    /// 有效的用户名
    /// </summary>
    public const string ValidUserName = "test_user";

    /// <summary>
    /// 空用户名（用于测试必填项校验）
    /// </summary>
    public const string EmptyUserName = "";

    /// <summary>
    /// 有效的密码（符合复杂度要求）
    /// </summary>
    public const string ValidPassword = "Password123?";

    /// <summary>
    /// 有效的新密码（用于测试密码修改场景）
    /// </summary>
    public const string ValidNewPassword = "NewPassword123?";

    /// <summary>
    /// 过短的密码（用于测试最小长度限制）
    /// </summary>
    public const string TooShortPassword = "123";

    /// <summary>
    /// 空密码（用于测试必填项校验）
    /// </summary>
    public const string EmptyPassword = "";

    /// <summary>
    /// 有效的电子邮件地址
    /// </summary>
    public const string ValidEmail = "test@example.com";

    /// <summary>
    /// 无效的电子邮件地址格式
    /// </summary>
    public const string InvalidEmail = "invalid-email";

    /// <summary>
    /// 有效的手机号码
    /// </summary>
    public const string ValidPhoneNumber = "13900000000";

    /// <summary>
    /// 有效的性别设置（保密/未知）
    /// </summary>
    public const AppUserSex ValidSex = AppUserSex.Secret;

    /// <summary>
    /// 有效的用户状态（启用）
    /// </summary>
    public const AppUserStatus ValidUserStatus = AppUserStatus.Enable;
}

