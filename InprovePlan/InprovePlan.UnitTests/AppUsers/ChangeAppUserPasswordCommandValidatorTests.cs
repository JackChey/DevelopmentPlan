using InprovePlan.UnitTests.Builders;
using InprovePlan.UnitTests.TestData;
using InprovePlan.UserCase.AppUsers.Commands;

namespace InprovePlan.UnitTests.AppUsers;

using FluentAssertions;
using Xunit;

/// <summary>
/// 修改用户密码命令 (ChangeAppUserPasswordCommand) 的验证器单元测试。
/// 主要覆盖以下业务规则：
/// 1. 所有字段合法时验证通过。
/// 2. 确认密码与新密码不一致时报错。
/// 3. 新密码与旧密码相同时报错（防止用户未实际修改密码）。
/// </summary>
public sealed class ChangeAppUserPasswordCommandValidatorTests
{
    // 实例化待测的验证器对象。
    // 由于验证器通常无状态或依赖注入简单，可以直接在此处初始化以供所有测试共用。
    private readonly ChangeAppUserPasswordCommandValidator _validator = new();

    /// <summary>
    /// 测试场景：修改密码命令的所有参数均合法。
    /// 预期结果：验证通过，无错误信息。
    /// </summary>
    [Fact]
    public void Validate_WhenCommandIsValid_ShouldPass()
    {
        // --- Arrange (准备阶段) ---
        // 使用构建器创建一个默认的合法命令对象。
        // 构建器内部应已设置了有效的 OldPassword, NewPassword, ConfirmPassword 等字段。
        var command = new ChangeAppUserPasswordCommandBuilder().Build();

        // --- Act (执行阶段) ---
        // 执行验证逻辑。
        var result = _validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言验证结果应为“通过”状态（即 IsValid 为 true，Errors 为空）。
        result.ShouldPassValidation();
    }

    /// <summary>
    /// 测试场景：确认密码 (ConfirmPassword) 与新密码 (NewPassword) 不一致。
    /// 预期结果：验证失败，且仅返回关于 ConfirmPassword 字段的错误。
    /// 目的：确保用户在输入新密码时没有因打字错误导致两次输入不匹配。
    /// </summary>
    [Fact]
    public void Validate_WhenConfirmPasswordIsDifferent_ShouldHaveConfirmPasswordValidationError()
    {
        // --- Arrange (准备阶段) ---
        // 构建命令：
        // 1. NewPassword 使用构建器默认的有效值（例如 "ValidPass123!"）。
        // 2. 显式将 ConfirmPassword 设置为一个不同的字符串 ("OtherPassword123?")。
        // 这种差异将触发“密码不匹配”的验证规则。
        var command = new ChangeAppUserPasswordCommandBuilder()
            .WithConfirmPassword("OtherPassword123?")
            .Build();

        // --- Act (执行阶段) ---
        var result = _validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言：验证结果中应恰好包含一个错误，且该错误明确指向 ConfirmPassword 属性。
        // 这确保了错误提示能准确引导用户检查确认密码输入框。
        result.ShouldHaveSingleValidationErrorFor(nameof(ChangeAppUserPasswordCommand.ConfirmPassword));
    }

    /// <summary>
    /// 测试场景：新密码 (NewPassword) 与旧密码 (OldPassword) 完全相同。
    /// 预期结果：验证失败，且仅返回关于 NewPassword 字段的错误。
    /// 目的：强制用户必须设置一个不同于当前密码的新密码，避免无效的操作请求。
    /// </summary>
    [Fact]
    public void Validate_WhenNewPasswordEqualsOldPassword_ShouldHaveNewPasswordValidationError()
    {
        // --- Arrange (准备阶段) ---
        // 构建命令：
        // 1. 获取测试数据中定义的“有效旧密码”（例如 "OldValidPass123!"）。
        // 2. 显式将 NewPassword 设置为与旧密码相同的值。
        // 3. 为了确保能通过“确认密码一致性”的检查，将 ConfirmPassword 也设置为相同的值。
        // 此时，虽然格式和一致性校验可能通过，但业务逻辑校验（新旧不同）应失败。
        var command = new ChangeAppUserPasswordCommandBuilder()
            .WithNewPassword(AppUserTestData.ValidPassword)
            .WithConfirmPassword(AppUserTestData.ValidPassword)
            .Build();

        // --- Act (执行阶段) ---
        var result = _validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言：验证结果中应恰好包含一个错误，且该错误明确指向 NewPassword 属性。
        // 这表明验证器成功识别了“新密码不能与旧密码相同”这一业务规则。
        result.ShouldHaveSingleValidationErrorFor(nameof(ChangeAppUserPasswordCommand.NewPassword));
    }
}

