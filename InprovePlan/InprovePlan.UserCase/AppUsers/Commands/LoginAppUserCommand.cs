using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using Instructure.Configurations.Entities;
using Instructure.Interfaces.Jwt;
using Instructure.IResult;
using Instructure.Repositories;
using Instructure.Specification;

namespace InprovePlan.UserCase.AppUsers.Commands;

/// <summary>
/// 用户登录命令。
///
/// 登录属于一次会改变“认证状态”的业务动作，
/// 所以这里使用 ICommand，而不是 IQuery。
/// </summary>
/// <param name="UserName">用户名。</param>
/// <param name="Password">用户明文密码。</param>
public sealed record LoginAppUserCommand(
    string UserName,
    string Password
) : ICommand<Result<LoginAppUserDto>>;

/// <summary>
/// 登录成功返回 DTO。
///
/// 生产环境中通常会返回：
/// - AccessToken
/// - RefreshToken
/// - ExpiresAt
///
/// </summary>
public sealed record LoginAppUserDto(
    string AccessToken);

/// <summary>
/// 用户登录参数校验。
///
/// 这里只校验“参数本身是否合法”：
/// - 用户名不能为空
/// - 用户名长度不能超过数据库字段长度
/// - 密码不能为空
///
/// 不在 Validator 中校验用户名是否存在、密码是否正确，
/// 因为这些需要查询数据库，应放在 Handler 中处理。
/// </summary>
public sealed class LoginAppUserCommandValidator
    : AbstractValidator<LoginAppUserCommand>
{
    public LoginAppUserCommandValidator()
    {
        RuleFor(command => command.UserName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("用户名不能为空。")
            .MaximumLength(DataSchemaConstants.UserNameLength)
            .WithMessage($"用户名长度不能超过 {DataSchemaConstants.UserNameLength} 个字符。");

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithMessage("密码不能为空。");
    }
}

/// <summary>
/// 根据用户名查询用户的 Specification。
///
/// 注意：
/// 1. 登录时不查询已删除用户。
/// 2. 用户名在 Handler 中已 Trim，这里直接使用标准化后的用户名。
/// 3. 条件最终会由 EF Core 翻译为数据库 WHERE。
/// </summary>
public sealed class AppUserByUserNameSpecification
    : Specification<AppUser>
{
    public AppUserByUserNameSpecification(string userName)
    {
        AddCriteria(user =>
            user.UserName == userName
            && !user.IsDeleted);
    }
}

/// <summary>
/// 用户登录命令处理器。
///
/// 处理流程：
/// 1. 标准化用户名。
/// 2. 根据用户名查询用户。
/// 3. 用户不存在时返回统一失败信息。
/// 4. 校验密码哈希。
/// 5. 密码错误时返回统一失败信息。
/// 6. 判断用户状态是否允许登录。
/// 7. 校验通过后返回 JWT。
///
/// 安全注意：
/// 用户名不存在和密码错误建议返回相同错误信息，
/// 避免攻击者通过响应差异枚举用户名。
/// </summary>
public sealed class LoginAppUserCommandHandler(
    IRepository<AppUser> appUserRepository,
    IJwtService jwtService,
    IPasswordHasher passwordHasher)
    : ICommandHandler<LoginAppUserCommand, Result<LoginAppUserDto>>
{
    public async Task<Result<LoginAppUserDto>> Handle(
        LoginAppUserCommand request,
        CancellationToken cancellationToken)
    {
        var userName = request.UserName.Trim();

        var appUser = await appUserRepository.FirstOrDefaultAsync(
            new AppUserByUserNameSpecification(userName),
            cancellationToken);

        if (appUser is null)
        {
            return Result<LoginAppUserDto>.Invalid("用户名或密码错误。");
        }

        var verifyResult = passwordHasher.Verify(
            appUser.PasswordHash,
            request.Password);

        if (verifyResult == PasswordVerifyResult.Failed)
        {
            return Result<LoginAppUserDto>.Invalid("用户名或密码错误。");
        }

        if (appUser.UserStatus != AppUserStatus.Enable)
        {
            return Result<LoginAppUserDto>.Forbidden("当前用户状态不允许登录。");
        }

        if (verifyResult == PasswordVerifyResult.SuccessRehashNeeded)
        {
            appUser.PasswordHash = passwordHasher.Hash(request.Password);

            appUserRepository.Update(appUser);

            await appUserRepository.SaveChangesAsync(cancellationToken);
        }

        var accessToken = jwtService.GetAccessToken(appUser);

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            return Result<LoginAppUserDto>.Success(
            new LoginAppUserDto(accessToken));
        }
        else
        {
            return Result<LoginAppUserDto>.Failure("创建Token失败");
        }
    }
}