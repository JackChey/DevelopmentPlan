using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using InprovePlan.UserCase.Common.Attributes;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.UserCase.AppUsers.Commands;

[RequireAuthorization]
public sealed record ChangeAppUserPasswordCommand(
    long Id,
    string OldPassword,
    string NewPassword,
    string ConfirmPassword
) : ICommand<Result>;

public sealed class ChangeAppUserPasswordCommandValidator
    : AbstractValidator<ChangeAppUserPasswordCommand>
{
    public ChangeAppUserPasswordCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0)
            .WithMessage("用户 ID 必须大于 0。");

        RuleFor(command => command.OldPassword)
            .NotEmpty()
            .WithMessage("原密码不能为空。");

        RuleFor(command => command.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("新密码不能为空。")
            .MinimumLength(8)
            .WithMessage("新密码长度不能少于 8 个字符。")
            .MaximumLength(128)
            .WithMessage("新密码长度不能超过 128 个字符。")
            .NotEqual(command => command.OldPassword)
            .WithMessage("新密码不能与原密码相同。");

        RuleFor(command => command.ConfirmPassword)
            .Equal(command => command.NewPassword)
            .WithMessage("两次输入的新密码不一致。");
    }
}

public sealed class ChangeAppUserPasswordCommandHandler(
    IRepository<AppUser> appUserRepository,
    IPasswordHasher passwordHasher)
    : ICommandHandler<ChangeAppUserPasswordCommand, Result>
{
    public async Task<Result> Handle(
        ChangeAppUserPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var appUser = await appUserRepository.GetByIdAsync(request.Id, cancellationToken);

        if (appUser is null || appUser.IsDeleted)
        {
            return Result.NotFound("用户不存在。");
        }

        if (appUser.UserStatus is AppUserStatus.Void or AppUserStatus.Frozen)
        {
            return Result.Forbidden("当前用户状态不允许修改密码。");
        }

        var verifyResult = passwordHasher.Verify(
            appUser.PasswordHash,
            request.OldPassword);

        if (verifyResult == PasswordVerifyResult.Failed)
        {
            return Result.Invalid("原密码不正确。");
        }

        appUser.PasswordHash = passwordHasher.Hash(request.NewPassword);

        //appUserRepository.Update(appUser);

        await appUserRepository.SaveChangesAsync(cancellationToken);

        return Result.SeccessWithNoMsg;
    }
}