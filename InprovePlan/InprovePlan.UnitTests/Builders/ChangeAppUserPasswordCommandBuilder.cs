using InprovePlan.UnitTests.TestData;
using InprovePlan.UserCase.AppUsers.Commands;

namespace InprovePlan.UnitTests.Builders;

/// <summary>
/// 修改用户密码命令 (ChangeAppUserPasswordCommand) 的测试数据构建器。
/// 用于在单元测试中快速构造合法或特定异常场景下的命令对象，
/// 避免在每个测试方法中重复编写繁琐的对象初始化代码。
/// </summary>
public sealed class ChangeAppUserPasswordCommandBuilder
{
    // 默认用户ID：初始化为测试数据中定义的有效用户ID。
    // 这确保了在未显式指定ID时，构建出的命令包含一个合法的标识符。
    private long _id = AppUserTestData.ValidUserId;

    // 默认旧密码：初始化为测试数据中定义的有效当前密码。
    // 用于模拟用户知道正确旧密码的正常场景。
    private string _oldPassword = AppUserTestData.ValidPassword;

    // 默认新密码：初始化为测试数据中定义的有效新密码。
    // 符合复杂度要求，用于模拟正常的密码更新操作。
    private string _newPassword = AppUserTestData.ValidNewPassword;

    // 默认确认密码：初始化为与默认新密码相同的值。
    // 确保在默认情况下，“新密码”与“确认密码”一致，通过一致性校验。
    private string _confirmPassword = AppUserTestData.ValidNewPassword;

    /// <summary>
    /// 设置确认密码。
    /// </summary>
    /// <param name="confirmPassword">要设置的确认密码字符串。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法常用于测试“密码不一致”的场景。
    /// 例如：传入与 newPassword 不同的字符串，以验证后端是否正确拦截了不匹配的确认密码。
    /// </remarks>
    public ChangeAppUserPasswordCommandBuilder WithConfirmPassword(string confirmPassword)
    {
        // 更新内部确认密码字段
        _confirmPassword = confirmPassword;

        // 返回 this 以支持链式调用
        return this;
    }

    /// <summary>
    /// 设置新密码。
    /// </summary>
    /// <param name="newPassword">要设置的新密码字符串。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法常用于测试密码复杂度校验或长度限制。
    /// 注意：如果修改了新密码但未同步修改确认密码，可能会导致“密码不一致”的验证错误，
    /// 这在测试中可能是有意为之（测试一致性校验），也可能是疏忽，需根据测试目的谨慎使用。
    /// </remarks>
    public ChangeAppUserPasswordCommandBuilder WithNewPassword(string newPassword)
    {
        // 更新内部新密码字段
        _newPassword = newPassword;

        // 返回 this 以支持链式调用
        return this;
    }

    /// <summary>
    /// 设置旧密码。
    /// </summary>
    /// <param name="oldPassword">要设置的旧密码字符串。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法常用于测试“旧密码错误”的场景。
    /// 例如：传入一个错误的密码字符串，以验证后端是否正确拒绝了非法的密码修改请求。
    /// </remarks>
    public ChangeAppUserPasswordCommandBuilder WithOldPassword(string oldPassword)
    {
        // 更新内部旧密码字段
        _oldPassword = oldPassword;

        // 返回 this 以支持链式调用
        return this;
    }

    /// <summary>
    /// 构建并返回最终的 ChangeAppUserPasswordCommand 对象。
    /// </summary>
    /// <returns>包含当前配置参数的命令对象实例。</returns>
    /// <remarks>
    /// 此方法是构建过程的终点。它使用当前内部存储的 _id, _oldPassword, _newPassword, _confirmPassword 
    /// 实例化命令对象。在此之后，构建器通常不再被使用，或者可以重置以构建新对象。
    /// </remarks>
    public ChangeAppUserPasswordCommand Build()
    {
        // 使用当前配置好的参数创建命令对象
        return new ChangeAppUserPasswordCommand(_id, _oldPassword, _newPassword, _confirmPassword);
    }
}
