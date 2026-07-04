using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.Exceptions;
using Instructure.Attributes;
using Instructure.Exceptions;
using Instructure.Interfaces;
using Instructure.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace InprovePlan.UserCase.Behaviors;

/// <summary>
/// MediatR 授权管道。
///
/// 作用：
/// 在进入 Handler 前，统一检查当前请求是否需要授权。
///
/// 处理流程：
/// 1. 检查 Request 类型上是否标记 RequireAuthorizationAttribute。
/// 2. 未标记则直接放行。
/// 3. 已标记则读取当前用户 ID。
/// 4. 用户 ID 不存在，抛 Unauthorized。
/// 5. 查询数据库用户。
/// 6. 用户不存在、已删除、被冻结、未启用，抛 Forbidden。
/// 7. 校验通过后执行下一个 Pipeline 或 Handler。
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse>(
    IUser currentUser,
    ILogger<AuthorizationBehavior<TRequest, TResponse>> logger,
    IReadRepository<AppUser> appUserRepository)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        var requiresAuthorization = request
            .GetType()
            .GetCustomAttributes<RequireAuthorizationAttribute>(inherit: true)
            .Any();

        if (!requiresAuthorization)
        {
            return await next();
        }

        if (currentUser.Id is null)
        {
            ThrowAuthorizationFailure(
                logger,
                AuthorizationFailureStatus.Unauthorized,
                code: "AUTH_USER_NOT_FOUND_IN_CONTEXT",
                message: "未检测到当前用户。");
        }

        var userId = currentUser.Id!.Value;

        var appUser = await appUserRepository.FirstOrDefaultAsNoTrackingAsync(
            user => user.Id == userId,
            cancellationToken);

        if (appUser is null)
        {
            ThrowAuthorizationFailure(
                logger,
                AuthorizationFailureStatus.Forbidden,
                code: "AUTH_USER_NOT_EXISTS",
                message: "当前用户不存在。");
        }

        if (appUser!.IsDeleted)
        {
            ThrowAuthorizationFailure(
                logger,
                AuthorizationFailureStatus.Forbidden,
                code: "AUTH_USER_DELETED",
                message: "当前用户已删除。");
        }

        if (appUser.UserStatus != AppUserStatus.Enable)
        {
            ThrowAuthorizationFailure(
                logger,
                AuthorizationFailureStatus.Forbidden,
                code: "AUTH_USER_DISABLED",
                message: "当前用户不可用。");
        }

        return await next();
    }

    /// <summary>
    /// 记录授权失败日志，并抛出授权异常。
    /// 
    /// 授权失败通常不是系统故障，
    /// 所以使用 Warning，而不是 Error。
    /// </summary>
    private void ThrowAuthorizationFailure(
        ILogger logger,
        AuthorizationFailureStatus status,
        string code,
        string message)
    {
        logger.LogWarning("Authorization Failed,event:{@event}, errorcode:{@errorcode}, msg:{@msg}", "authorization_failed", code, message);

        throw new AuthorizationException(
            status,
            code,
            message);
    }
}
