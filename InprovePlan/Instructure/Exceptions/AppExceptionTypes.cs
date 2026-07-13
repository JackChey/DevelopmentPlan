namespace Instructure.Exceptions;

/// <summary>
/// 系统基础异常
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>
    /// 异常编码
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;


    /// <summary>
    /// 传入异常信息
    /// </summary>
    /// <param name="message"></param>
    /// <param name="errorcode"></param>
    public AppException(string message, string errorcode) : base(message)
    {
        ErrorCode = errorcode;
    }
}

/// <summary>
/// 验证异常
/// </summary>
public sealed class ValidationException : AppException
{
    //public new string ErrorCode = "validate_failed";

    /// <summary>
    /// 异常信息集合
    /// </summary>
    public IDictionary<string, string[]> Errors { get; set; }

    /// <summary>
    /// 传入异常信息
    /// </summary>
    /// <param name="errors"></param>
    /// <param name="message"></param>
    public ValidationException(IDictionary<string, string[]> errors, string message = "Validate failed") : base(message, "validate_failed")
    {
        Errors = errors;
    }
}

/// <summary>
/// 资源异常
/// </summary>
public sealed class NotFoundException : AppException
{
    //public new string ErrorCode = "resource_notfound";

    /// <summary>
    /// 传入异常信息
    /// </summary>
    /// <param name="errorcode"></param>
    /// <param name="message"></param>
    public NotFoundException(string message, string errorcode = "resource_not_found") : base(message, errorcode)
    {

    }
}

/// <summary>
/// 业务异常
/// </summary>
public sealed class BusinessException : AppException
{
    //public new string ErrorCode = "business_exception";

    /// <summary>
    /// 传入异常信息
    /// </summary>
    /// <param name="errorcode"></param>
    /// <param name="message"></param>
    public BusinessException(string message, string errorcode = "business_rule_violation") : base(message, errorcode)
    {

    }
}

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

/// <summary>
/// 
/// </summary>
public sealed class IdempotencyException : Exception
{
    public IdempotencyException(
        IdempotencyFailureStatus status,
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
    public IdempotencyFailureStatus Status { get; }

    /// <summary>
    /// 业务错误码。
    /// </summary>
    public string Code { get; }
}

/// <summary>
/// 幂等操作失败状态
/// </summary>
public enum IdempotencyFailureStatus
{
    BadRequest = 400,
    Conflict‌ = 409,
    NotFound = 404,
}
