using InprovePlan.UnitTests.Builders;
using InprovePlan.UnitTests.TestData;
using InprovePlan.UserCase.AppUsers.Commands;

namespace InprovePlan.UnitTests.AppUsers;

using FluentAssertions;
using Xunit;

/// <summary>
/// 修改用户基础信息命令 (UpdateAppUserCommand) 的验证器单元测试。
/// 主要覆盖以下核心字段的校验逻辑：
/// 1. Id: 必须为有效的用户标识符（通常大于0）。
/// 2. Email: 必须符合标准的电子邮件格式。
/// </summary>
public sealed class UpdateAppUserCommandValidatorTests
{
    // 实例化待测的验证器对象。
    // 由于验证器通常是无状态的，可以在测试类级别初始化，供所有测试方法复用，提高执行效率。
    private readonly UpdateAppUserCommandValidator _validator = new();

    /// <summary>
    /// 测试场景：修改用户信息命令的所有参数均合法。
    /// 预期结果：验证通过，无错误信息。
    /// </summary>
    [Fact]
    public void Validate_WhenCommandIsValid_ShouldPass()
    {
        // --- Arrange (准备阶段) ---
        // 使用构建器创建一个默认的合法命令对象。
        // 构建器内部应已设置了有效的 UserId 和符合格式的 Email。
        var command = new UpdateAppUserCommandBuilder().Build();

        // --- Act (执行阶段) ---
        // 执行验证逻辑。
        var result = _validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言验证结果应为“通过”状态（即 IsValid 为 true，Errors 集合为空）。
        // 这确保了在正常路径下，验证器不会误报任何错误，允许请求进入后续的业务处理流程。
        result.ShouldPassValidation();
    }

    /// <summary>
    /// 测试场景：用户ID (Id) 无效（如为0、负数或超出范围）。
    /// 预期结果：验证失败，且仅返回关于 Id 字段的错误。
    /// 目的：确保系统只能更新已存在的、具有有效标识的用户记录，防止非法的数据操作。
    /// </summary>
    [Fact]
    public void Validate_WhenIdIsInvalid_ShouldHaveIdValidationError()
    {
        // --- Arrange (准备阶段) ---
        // 构建命令：
        // 显式将 Id 设置为测试数据中定义的无效值（InvalidUserId）。
        // Email 保持构建器默认的合法值，以隔离变量，确保错误仅由 Id引起。
        var command = new UpdateAppUserCommandBuilder()
            .WithId(AppUserTestData.InvalidUserId)
            .Build();

        // --- Act (执行阶段) ---
        var result = _validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言：验证结果中应恰好包含一个错误，且该错误明确指向 Id 属性。
        // 这确保了前端或调用方能准确识别出是用户标识有问题。
        result.ShouldHaveSingleValidationErrorFor(nameof(UpdateAppUserCommand.Id));
    }

    /// <summary>
    /// 测试场景：邮箱地址 (Email) 格式无效（如缺少 '@' 符号或域名部分）。
    /// 预期结果：验证失败，且仅返回关于 Email 字段的错误。
    /// 目的：确保系统中存储的用户联系信息格式正确，以便后续发送通知或进行身份找回。
    /// </summary>
    [Fact]
    public void Validate_WhenEmailIsInvalid_ShouldHaveEmailValidationError()
    {
        // --- Arrange (准备阶段) ---
        // 构建命令：
        // 显式将 Email 设置为测试数据中定义的无效邮箱格式（InvalidEmail）。
        // Id 保持构建器默认的合法值，以隔离变量，确保错误仅由 Email 引起。
        var command = new UpdateAppUserCommandBuilder()
            .WithEmail(AppUserTestData.InvalidEmail)
            .Build();

        // --- Act (执行阶段) ---
        var result = _validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言：验证结果中应恰好包含一个错误，且该错误明确指向 Email 属性。
        // 这确保了只有符合标准 RFC 格式的邮箱才能被接受并更新到数据库中。
        result.ShouldHaveSingleValidationErrorFor(nameof(UpdateAppUserCommand.Email));
    }
}

