using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using Instructure.Attributes;
using Instructure.Configurations.Entities;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.UserCase.AppUsers.Commands;

/// <summary>
/// 修改用户基础信息命令。
///
/// 注意：
/// 这里不修改 PasswordHash。
/// 修改密码应单独设计 ChangePasswordCommand，
/// 因为密码修改有独立的安全规则。
/// </summary>
[RequireAuthorization]
public sealed record UpdateAppUserCommand(
    long Id,
    string UserName,
    string Email,
    string? PhoneNumber,
    AppUserSex Sex,
    AppUserStatus UserStatus
) : ICommand<Result<AppUserDto>>;

public sealed class UpdateAppUserCommandValidator
    : AbstractValidator<UpdateAppUserCommand>
{
    public UpdateAppUserCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0)
            .WithMessage("用户 ID 必须大于 0。");

        RuleFor(command => command.UserName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("用户名不能为空。")
            .MaximumLength(DataSchemaConstants.UserNameLength)
            .WithMessage($"用户名长度不能超过 {DataSchemaConstants.UserNameLength} 个字符。");

        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("邮箱不能为空。")
            .EmailAddress()
            .WithMessage("邮箱格式不正确。")
            .MaximumLength(DataSchemaConstants.EmailLength)
            .WithMessage($"邮箱长度不能超过 {DataSchemaConstants.EmailLength} 个字符。");

        RuleFor(command => command.PhoneNumber)
            .MaximumLength(DataSchemaConstants.PhoneNumberLength)
            .WithMessage($"手机号长度不能超过 {DataSchemaConstants.PhoneNumberLength} 个字符。")
            .When(command => !string.IsNullOrWhiteSpace(command.PhoneNumber));

        RuleFor(command => command.Sex)
            .IsInEnum()
            .WithMessage("性别参数不合法。");

        RuleFor(command => command.UserStatus)
            .IsInEnum()
            .WithMessage("用户状态参数不合法。");
    }
}

public sealed class UpdateAppUserCommandHandler(
    IRepository<AppUser> appUserRepository)
    : ICommandHandler<UpdateAppUserCommand, Result<AppUserDto>>
{
    public async Task<Result<AppUserDto>> Handle(
        UpdateAppUserCommand request,
        CancellationToken cancellationToken)
    {
        var appUser = await appUserRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (appUser is null || appUser.IsDeleted)
        {
            return Result<AppUserDto>.NotFound("用户不存在。");
        }

        var userName = request.UserName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;

        var userNameExists = await appUserRepository.AnyAsync(
            user => user.Id != request.Id
                    && user.UserName == userName
                    && !user.IsDeleted,
            cancellationToken);

        if (userNameExists)
        {
            return Result<AppUserDto>.Conflict("用户名已存在。");
        }

        appUser.UserName = userName;
        appUser.Email = email;
        appUser.PhoneNumber = phoneNumber;
        appUser.Sex = request.Sex;
        appUser.UserStatus = request.UserStatus;

        //appUserRepository.Update(appUser);

        await appUserRepository.SaveChangesAsync(cancellationToken);

        var dto = new AppUserDto(
            Id: appUser.Id,
            UserName: appUser.UserName,
            Email: appUser.Email,
            PhoneNumber: appUser.PhoneNumber,
            Sex: appUser.Sex,
            UserStatus: appUser.UserStatus);

        return Result<AppUserDto>.Success(dto);
    }
}