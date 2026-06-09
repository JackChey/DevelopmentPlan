using FluentAssertions;
using InprovePlan.UserCase.AppUsers.Commands;
using Xunit;

namespace InprovePlan.UnitTests.AppUsers;

/// <summary>
/// 修改用户密码参数校验测试。
/// </summary>
public sealed class ChangeAppUserPasswordCommandValidatorTests
{
    private readonly ChangeAppUserPasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        var command = new ChangeAppUserPasswordCommand(
            1,
            "Password123?",
            "NewPassword123?",
            "NewPassword123?");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenConfirmPasswordIsDifferent()
    {
        var command = new ChangeAppUserPasswordCommand(
            1,
            "Password123?",
            "NewPassword123?",
            "OtherPassword123?");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldFail_WhenNewPasswordEqualsOldPassword()
    {
        var command = new ChangeAppUserPasswordCommand(
            1,
            "Password123?",
            "Password123?",
            "Password123?");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}