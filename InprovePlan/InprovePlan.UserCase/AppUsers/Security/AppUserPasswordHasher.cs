using InprovePlan.Domain.Entities;
using Instructure.IResult;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;

namespace InprovePlan.UserCase.AppUsers.Security;

/// <summary>
/// 基于 ASP.NET Core Identity 的密码哈希实现。
///
/// 说明：
/// 1. 不自己实现密码加密算法。
/// 2. 使用 Microsoft.AspNetCore.Identity.PasswordHasher<TUser>。
/// 3. 默认使用 PBKDF2，属于 ASP.NET Core 生态中的主流生产方案。
/// 4. 支持验证旧哈希是否需要重新哈希。
///
/// 为什么不用 MD5 / SHA256：
/// 普通摘要算法太快，不适合存储密码。
/// 密码存储应使用专门的慢哈希算法，例如 PBKDF2、BCrypt、Argon2。
/// </summary>
public sealed class AppUserPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// ASP.NET Core Identity 官方密码哈希器。
    /// </summary>
    private readonly PasswordHasher<AppUser> _passwordHasher;

    /// <summary>
    /// 创建密码哈希服务。
    ///
    /// PasswordHasherOptions 可以通过 DI 配置。
    /// </summary>
    public AppUserPasswordHasher(
        IOptions<PasswordHasherOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _passwordHasher = new PasswordHasher<AppUser>(options);
    }

    /// <inheritdoc />
    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("密码不能为空。", nameof(password));
        }

        // PasswordHasher<TUser> 的 user 参数可用于高级场景。
        // 默认实现不依赖 AppUser 的字段。
        var user = new AppUser();

        return _passwordHasher.HashPassword(user, password);
    }

    /// <inheritdoc />
    public PasswordVerifyResult Verify(
        string passwordHash,
        string password)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return PasswordVerifyResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return PasswordVerifyResult.Failed;
        }

        var user = new AppUser();

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            passwordHash,
            password);

        return result switch
        {
            PasswordVerificationResult.Success
                => PasswordVerifyResult.Success,

            PasswordVerificationResult.SuccessRehashNeeded
                => PasswordVerifyResult.SuccessRehashNeeded,

            _ => PasswordVerifyResult.Failed
        };
    }
}

