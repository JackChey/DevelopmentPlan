using System;
using System.Runtime.Serialization;

namespace Instructure.Exceptions
{
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
}
