using FluentAssertions;
using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.AppUsers.Commands;
using Xunit;

namespace InprovePlan.UnitTests.AppUsers;

/// <summary>
/// 修改用户基础信息参数校验测试。
/// </summary>
public sealed class UpdateAppUserCommandValidatorTests
{
    private readonly UpdateAppUserCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        var command = new UpdateAppUserCommand(
            1,
            "test_user",
            "test@example.com",
            "13900000000",
            AppUserSex.Secret,
            AppUserStatus.Enable);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenIdIsInvalid()
    {
        var command = new UpdateAppUserCommand(
            0,
            "test_user",
            "test@example.com",
            "13900000000",
            AppUserSex.Secret,
            AppUserStatus.Enable);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldFail_WhenEmailIsInvalid()
    {
        var command = new UpdateAppUserCommand(
            1,
            "test_user",
            "bad-email",
            "13900000000",
            AppUserSex.Secret,
            AppUserStatus.Enable);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}