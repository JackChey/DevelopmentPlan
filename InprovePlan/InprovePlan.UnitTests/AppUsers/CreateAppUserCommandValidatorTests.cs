using InprovePlan.UnitTests.Builders;
using InprovePlan.UnitTests.TestData;
using InprovePlan.UserCase.AppUsers.Commands;

namespace InprovePlan.UnitTests.AppUsers;

using Xunit;

/// <summary>
/// 创建用户命令 (CreateAppUserCommand) 的验证器单元测试。
/// 主要覆盖以下核心字段的校验逻辑：
/// 1. UserName: 非空检查。
/// 2. Password: 长度及复杂度检查。
/// 3. Email: 格式合法性检查。
/// </summary>
public sealed class CreateAppUserCommandValidatorTests
{
    // 实例化待测的验证器对象。
    // 由于验证器通常是无状态的，可以在测试类级别初始化，供所有测试方法复用，提高执行效率。
    private readonly CreateAppUserCommandValidator _validator = new();

    /// <summary>
    /// 测试场景：创建用户命令的所有参数均合法。
    /// 预期结果：验证通过，无错误信息。
    /// </summary>
    [Fact]
    public void Validate_WhenCommandIsValid_ShouldPass()
    {
        // --- Arrange (准备阶段) ---
        // 使用构建器创建一个默认的合法命令对象。
        // 构建器内部应已设置了符合规则的用户名、强密码和有效邮箱地址。
        var command = new CreateAppUserCommandBuilder().Build();

        // --- Act (执行阶段) ---
        // 执行验证逻辑。
        var result = _validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言验证结果应为“通过”状态（即 IsValid 为 true，Errors 集合为空）。
        // 这确保了在正常路径下，验证器不会误报任何错误。
        result.ShouldPassValidation();
    }

    /// <summary>
    /// 测试场景：用户名为空字符串或 null。
    /// 预期结果：验证失败，且仅返回关于 UserName 字段的错误。
    /// 目的：确保每个用户账户必须拥有一个有效的标识名称。
    /// </summary>
    [Fact]
    public void Validate_WhenUserNameIsEmpty_ShouldHaveUserNameValidationError()
    {
        // --- Arrange (准备阶段) ---
        // 构建命令：
        // 显式将 UserName 设置为测试数据中定义的空值（EmptyUserName）。
        // 其他字段（Password, Email）保持构建器默认的合法值，以隔离变量。
        var command = new CreateAppUserCommandBuilder()
            .WithUserName(AppUserTestData.EmptyUserName)
            .Build();

        // --- Act (执行阶段) ---
        var result = _validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言：验证结果中应恰好包含一个错误，且该错误明确指向 UserName 属性。
        // 这确保了前端能准确提示用户“用户名不能为空”。
        result.ShouldHaveSingleValidationErrorFor(nameof(CreateAppUserCommand.UserName));
    }

    /// <summary>
    /// 测试场景：密码长度过短，不满足最小长度要求。
    /// 预期结果：验证失败，且仅返回关于 Password 字段的错误。
    /// 目的：强制用户设置足够复杂的密码以保障账户安全。
    /// </summary>
    [Fact]
    public void Validate_WhenPasswordIsTooShort_ShouldHavePasswordValidationError()
    {
        // --- Arrange (准备阶段) ---
        // 构建命令：
        // 显式将 Password 设置为测试数据中定义的过短密码（TooShortPassword）。
        // 注意：此处未修改 UserName 和 Email，确保它们处于合法状态，从而孤立密码验证逻辑。
        var command = new CreateAppUserCommandBuilder()
            .WithPassword(AppUserTestData.TooShortPassword)
            .Build();

        // --- Act (执行阶段) ---
        var result = _validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言：验证结果中应恰好包含一个错误，且该错误明确指向 Password 属性。
        // 这表明验证器成功拦截了弱密码尝试。
        result.ShouldHaveSingleValidationErrorFor(nameof(CreateAppUserCommand.Password));
    }

    /// <summary>
    /// 测试场景：邮箱地址格式无效（如缺少 '@' 符号或域名部分）。
    /// 预期结果：验证失败，且仅返回关于 Email 字段的错误。
    /// 目的：确保系统存储的邮箱地址格式正确，以便后续发送通知或重置密码链接。
    /// </summary>
    [Fact]
    public void Validate_WhenEmailIsInvalid_ShouldHaveEmailValidationError()
    {
        // --- Arrange (准备阶段) ---
        // 构建命令：
        // 显式将 Email 设置为测试数据中定义的无效邮箱格式（InvalidEmail）。
        var command = new CreateAppUserCommandBuilder()
            .WithEmail(AppUserTestData.InvalidEmail)
            .Build();

        // --- Act (执行阶段) ---
        var result = _validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言：验证结果中应恰好包含一个错误，且该错误明确指向 Email 属性。
        // 这确保了只有符合标准 RFC 格式的邮箱才能被接受。
        result.ShouldHaveSingleValidationErrorFor(nameof(CreateAppUserCommand.Email));
    }
}

