using InprovePlan.UnitTests.Builders;
using InprovePlan.UnitTests.TestData;
using InprovePlan.UserCase.AppUsers.Commands;

namespace InprovePlan.UnitTests.AppUsers;

using FluentAssertions;
using Xunit;

/// <summary>
/// 登录用户命令 (LoginAppUserCommand) 的验证器单元测试。
/// 主要覆盖以下核心字段的校验逻辑：
/// 1. UserName: 非空检查，确保用户提供了身份标识。
/// 2. Password: 非空检查，确保用户提供了凭证。
/// </summary>
public sealed class LoginAppUserCommandValidatorTests
{
    // 实例化待测的验证器对象。
    // 由于验证器通常是无状态的，可以在测试类级别初始化，供所有测试方法复用，提高执行效率。
    private readonly LoginAppUserCommandValidator _validator = new();

    /// <summary>
    /// 测试场景：登录命令的所有参数均合法。
    /// 预期结果：验证通过，无错误信息。
    /// </summary>
    [Fact]
    public void Validate_WhenCommandIsValid_ShouldPass()
    {
        // --- Arrange (准备阶段) ---
        // 使用构建器创建一个默认的合法命令对象。
        // 构建器内部应已设置了有效的 UserName 和 Password。
        var command = new LoginAppUserCommandBuilder().Build();

        // --- Act (执行阶段) ---
        // 执行验证逻辑。
        var result = _validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言验证结果应为“通过”状态（即 IsValid 为 true，Errors 集合为空）。
        // 这确保了在正常路径下，验证器不会误报任何错误，允许请求进入后续的身份认证流程。
        result.ShouldPassValidation();
    }

    /// <summary>
    /// 测试场景：用户名为空字符串或 null。
    /// 预期结果：验证失败，且仅返回关于 UserName 字段的错误。
    /// 目的：防止用户提交空的身份信息，确保后端能接收到有效的查询键。
    /// </summary>
    [Fact]
    public void Validate_WhenUserNameIsEmpty_ShouldHaveUserNameValidationError()
    {
        // --- Arrange (准备阶段) ---
        // 构建命令：
        // 显式将 UserName 设置为测试数据中定义的空值（EmptyUserName）。
        // Password 保持构建器默认的合法值，以隔离变量，确保错误仅由 UserName 引起。
        var command = new LoginAppUserCommandBuilder()
            .WithUserName(AppUserTestData.EmptyUserName)
            .Build();

        // --- Act (执行阶段) ---
        var result = _validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言：验证结果中应恰好包含一个错误，且该错误明确指向 UserName 属性。
        // 这确保了前端能准确提示用户“用户名不能为空”。
        result.ShouldHaveSingleValidationErrorFor(nameof(LoginAppUserCommand.UserName));
    }

    /// <summary>
    /// 测试场景：密码为空字符串或 null。
    /// 预期结果：验证失败，且仅返回关于 Password 字段的错误。
    /// 目的：防止用户提交空的凭证，确保每次登录尝试都包含完整的认证信息。
    /// </summary>
    [Fact]
    public void Validate_WhenPasswordIsEmpty_ShouldHavePasswordValidationError()
    {
        // --- Arrange (准备阶段) ---
        // 构建命令：
        // 显式将 Password 设置为测试数据中定义的空值（EmptyPassword）。
        // UserName 保持构建器默认的合法值，以隔离变量，确保错误仅由 Password 引起。
        var command = new LoginAppUserCommandBuilder()
            .WithPassword(AppUserTestData.EmptyPassword)
            .Build();

        // --- Act (执行阶段) ---
        var result = _validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言：验证结果中应恰好包含一个错误，且该错误明确指向 Password 属性。
        // 这确保了前端能准确提示用户“密码不能为空”。
        result.ShouldHaveSingleValidationErrorFor(nameof(LoginAppUserCommand.Password));
    }
}

