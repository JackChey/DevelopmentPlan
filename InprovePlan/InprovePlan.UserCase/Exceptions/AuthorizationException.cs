using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InprovePlan.UserCase.Exceptions;

/// <summary>
/// 授权异常。
/// 
/// 用于 MediatR AuthorizationBehavior 中表达：
/// - 未登录
/// - 当前用户不存在
/// - 当前用户已删除
/// - 当前用户不可用
/// </summary>
public sealed class AuthorizationException : Exception
{
    public AuthorizationException(
        AuthorizationFailureStatus status,
        string code,
        string message)
        : base(message)
    {
        Status = status;
        Code = code;
    }

    /// <summary>
    /// 授权失败状态。
    /// </summary>
    public AuthorizationFailureStatus Status { get; }

    /// <summary>
    /// 业务错误码。
    /// </summary>
    public string Code { get; }
}

public enum AuthorizationFailureStatus
{
    Unauthorized = 401,
    Forbidden = 403
}