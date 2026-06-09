using FluentAssertions;
using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.AppUsers.Commands;
using Xunit;

namespace InprovePlan.UnitTests.AppUsers;

/// <summary>
/// 创建用户参数校验测试。
/// </summary>
public sealed class CreateAppUserCommandValidatorTests
{
    private readonly CreateAppUserCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        var command = new CreateAppUserCommand(
            "test_user",
            "Password123?",
            AppUserSex.Secret,
            "13900000000",
            "test@example.com");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenUserNameIsEmpty()
    {
        var command = new CreateAppUserCommand(
            "",
            "Password123?",
            AppUserSex.Secret,
            "13900000000",
            "test@example.com");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordIsTooShort()
    {
        var command = new CreateAppUserCommand(
            "test_user",
            "123",
            AppUserSex.Secret,
            "13900000000",
            "test@example.com");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldFail_WhenEmailIsInvalid()
    {
        var command = new CreateAppUserCommand(
            "test_user",
            "Password123?",
            AppUserSex.Secret,
            "13900000000",
            "invalid-email");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}