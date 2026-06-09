using FluentAssertions;
using InprovePlan.UserCase.AppUsers.Commands;
using Xunit;

namespace InprovePlan.UnitTests.AppUsers;

/// <summary>
/// 登录参数校验测试。
/// </summary>
public sealed class LoginAppUserCommandValidatorTests
{
    private readonly LoginAppUserCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = _validator.Validate(
            new LoginAppUserCommand("test_user", "Password123?"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenUserNameIsEmpty()
    {
        var result = _validator.Validate(
            new LoginAppUserCommand("", "Password123?"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordIsEmpty()
    {
        var result = _validator.Validate(
            new LoginAppUserCommand("test_user", ""));

        result.IsValid.Should().BeFalse();
    }
}