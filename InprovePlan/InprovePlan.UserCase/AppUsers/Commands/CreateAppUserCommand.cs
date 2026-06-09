using AutoMapper;
using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using Instructure.Configurations.Entities;
using Instructure.Interfaces;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.UserCase.AppUsers.Commands;

/// <summary>
/// 新增用户命令参数校验。
///
/// FluentValidation 主要负责“参数本身是否合法”：
/// - 是否为空
/// - 长度是否超限
/// - 邮箱格式是否正确
/// - 枚举值是否合法
///
/// 不建议在这里放太复杂的业务逻辑。
/// 例如“用户名是否重复”可以放 Handler 中查询数据库判断。
/// </summary>
public sealed class CreateAppUserCommandValidator
    : AbstractValidator<CreateAppUserCommand>
{
    /// <summary>
    /// 校验逻辑
    /// </summary>
    public CreateAppUserCommandValidator()
    {
        RuleFor(command => command.UserName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("用户名不能为空。")
            .MaximumLength(DataSchemaConstants.UserNameLength)
            .WithMessage($"用户名长度不能超过 {DataSchemaConstants.UserNameLength} 个字符。");

        RuleFor(command => command.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("密码不能为空。")
            .MinimumLength(8)
            .WithMessage("密码长度不能少于 8 个字符。")
            .MaximumLength(128)
            .WithMessage("密码长度不能超过 128 个字符。");

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
    }
}


/// <summary>
/// 
/// </summary>
/// <param name="UserName"></param>
/// <param name="Password"></param>
/// <param name="Sex"></param>
/// <param name="PhoneNumber"></param>
/// <param name="Email"></param>
public record CreateAppUserCommand(
    string UserName,
   string Password,
   AppUserSex Sex,
   string PhoneNumber,
   string Email
   ) : ICommand<Result<AppUserDto>>
{

}


/// <summary>
/// 新增用户命令处理器。
///
/// 处理流程：
/// 1. 标准化输入。
/// 2. 查询用户名是否已存在。
/// 3. 查询邮箱是否已存在。 -- 去除检验
/// 4. 对密码进行哈希。
/// 5. 创建 AppUser 实体。
/// 6. 调用仓储保存。
/// 7. 返回安全的 DTO。
/// </summary>
public sealed class CreateAppUserCommandHandler(
    IRepository<AppUser> appUserRepository,
    IMapper mapper,
    IPasswordHasher passwordHasher)
    : ICommandHandler<CreateAppUserCommand, Result<AppUserDto>>
{
    /// <summary>
    /// 处理逻辑
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<AppUserDto>> Handle(
        CreateAppUserCommand request,
        
        CancellationToken cancellationToken)
    {
        var userName = request.UserName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;

        var userNameExists = await appUserRepository.AnyAsync(
            user => user.UserName == userName && !user.IsDeleted,
            cancellationToken);

        if (userNameExists)
        {
            return Result.Failure("用户名已存在。");
        }

        var passwordHash = passwordHasher.Hash(request.Password);

        var appUser = new AppUser
        {
            UserName = userName,
            Email = email,
            PhoneNumber = phoneNumber,
            Sex = request.Sex,
            PasswordHash = passwordHash,
            UserStatus = AppUserStatus.Enable,
            IsDeleted = false,
            DeletedAt = null
        };

         var dto = await appUserRepository.AddAsync(appUser);

        await appUserRepository.SaveChangesAsync(cancellationToken);

        return Result<AppUserDto>.Success(mapper.Map<AppUserDto>(dto));
    }
}
