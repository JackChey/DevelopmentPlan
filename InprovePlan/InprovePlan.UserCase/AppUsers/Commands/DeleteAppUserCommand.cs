using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using InprovePlan.UserCase.Common.Attributes;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.UserCase.AppUsers.Commands;

/// <summary>
/// 删除用户命令。
///
/// 这里采用软删除：
/// - IsDeleted = true
/// - DeletedAt = 当前时间
///
/// 生产环境中，用户数据通常不建议物理删除，
/// 否则会影响审计、订单、日志、历史数据关联。
/// </summary>
[RequireAuthorization]
public sealed record DeleteAppUserCommand(long Id) : ICommand<Result>;

public sealed class DeleteAppUserCommandValidator
    : AbstractValidator<DeleteAppUserCommand>
{
    public DeleteAppUserCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0)
            .WithMessage("用户 ID 必须大于 0。");
    }
}

public sealed class DeleteAppUserCommandHandler(
    IRepository<AppUser> appUserRepository)
    : ICommandHandler<DeleteAppUserCommand, Result>
{
    public async Task<Result> Handle(
        DeleteAppUserCommand request,
        CancellationToken cancellationToken)
    {
        var appUser = await appUserRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (appUser is null || appUser.IsDeleted)
        {
            return Result.NotFound("用户不存在。");
        }

        appUser.IsDeleted = true;
        appUser.DeletedAt = DateTimeOffset.UtcNow;
        appUser.UserStatus = AppUserStatus.Void;

        //appUserRepository.Update(appUser);

        await appUserRepository.SaveChangesAsync(cancellationToken);

        return Result.SeccessWithNoMsg;
    }
}