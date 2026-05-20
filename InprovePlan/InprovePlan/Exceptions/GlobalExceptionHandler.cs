using Microsoft.AspNetCore.Diagnostics;

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Instructure.IResult;
using Microsoft.OpenApi.Models;
using Instructure.Response;
using InprovePlan.Helper;
using Serilog;
using Microsoft.Extensions.Logging;

namespace InprovePlan.Exceptions
{
    /// <summary>
    /// 全局异常捕捉处理
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _env;

        /// <summary>
        /// 构造函数,需传入日志组件以及系统环境
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="env"></param>
        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// 异常处理
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="exception"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>true表示已完成处理,false表示未完成处理</returns>
        /// <exception cref="NotImplementedException"></exception>
        async ValueTask<bool> IExceptionHandler.TryHandleAsync(HttpContext httpContext, System.Exception exception, CancellationToken cancellationToken)
        {
            // 获取异常信息
            var (resultstatus, errorcode, message, details) = Map(exception, _env.IsDevelopment());
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            var statusCode = resultstatus.ToHttpStatusCode();

            // 若异常状态大于等于 500 ,则代表重大异常,需要进行记录,
            if (statusCode >= 500)
            {
                _logger.LogError(exception, "Event:{event},ErrorCode:{errorcode},Unhandled bussiness exception.TraceId={TraceId},Msg:{}", "http.request.failed", errorcode, Activity.Current?.Id ?? httpContext.TraceIdentifier, "Unhandled_Exception");
            }
            else
            {
                _logger.LogWarning(exception, "Handled bussiness exception.TraceId={TraceId},Msg:{}", Activity.Current?.Id ?? httpContext.TraceIdentifier, "Handled_Exception");
            }

            var response = ApiResponse<object?>.Fail(errorcode, message, traceId, details);

            httpContext.Response.StatusCode = statusCode;

            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            return true;
        }


        /// <summary>
        /// 拆分异常信息标准化
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="isDevelopment"></param>
        /// <returns></returns>
        private static (ResultStatus status, string errorCode, string message, IEnumerable<string>? details) Map(Exception ex, bool isDevelopment) => ex switch
        {
            ValidationException vex => (
                ResultStatus.Invalid,
                vex.ErrorCode,
                vex.Message,
                FlattenValidationErrors(vex.Errors)
            ),

            NotFoundException nex => (
                ResultStatus.NotFound,
                nex.ErrorCode,
                nex.Message,
                null
            ),

            BusinessException bex => (
                ResultStatus.Conflict,
                bex.ErrorCode,
                bex.Message,
                null
            ),

            UnauthorizedAccessException => (
                ResultStatus.Forbidden,
                "forbidden",
                "Forbidden",
                null
            ),

            OperationCanceledException => (
                ResultStatus.Error,
                "client_closed_request",
                "Client closed request",
                null
            ),

            _ => (
                ResultStatus.Error,
                "internal_error",
                isDevelopment ? ex.Message : "An unexpected error occurred.",
                null
            )
        };

        private static IEnumerable<string> FlattenValidationErrors(IDictionary<string, string[]> errors)
        {
            foreach (var (key, values) in errors)
            {
                if (values == null || values.Length == 0) continue;

                foreach (var value in values)
                    yield return string.IsNullOrWhiteSpace(key) ? value : $"{key}: {value}";
            }
        }

    }


}
