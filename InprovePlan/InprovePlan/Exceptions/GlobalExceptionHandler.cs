using InprovePlan.Helper;
using Instructure.Exceptions;
using Instructure.IResult;
using Instructure.Response;
using Instructure.SystemLogs;
using Microsoft.AspNetCore.Diagnostics;
using System.Diagnostics;

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
        async ValueTask<bool> IExceptionHandler.TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var showDetails = _env.IsDevelopment()
    ||      _env.IsEnvironment("Testing");

            // 获取异常信息
            var (resultstatus, errorcode, message, details) = Map(exception, showDetails);
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            var statusCode = resultstatus.ToHttpStatusCode();

            // 获取请求信息
            var http = new LogHttpRequestInfo()
            {
                Route = httpContext.Request.Path,
                Method = httpContext.Request.Method,
                StatusCode = statusCode,
                ClientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unkown",
            };

            httpContext.Items.TryGetValue("auth", out var auth);

            // 若异常状态大于等于 500 ,则代表重大异常,需要进行记录,
            if (statusCode >= 500)
            {
                _logger.LogError(exception, "Event:{@event},Http:{@http},Auth:{@auth},ErrorCode:{@errorcode},Unhandled bussiness exception.TraceId={@traceId},Msg:{@msg}", LogEvents.ExceptionUnhandled, http, auth, errorcode, Activity.Current?.Id ?? httpContext.TraceIdentifier, "Unhandled_Exception");
            }
            else
            {
                _logger.LogWarning(exception, "Event:{@event},Http:{@http},Auth:{@auth},ErrorCode:{@errorcode},Handled bussiness exception.TraceId={@traceId},Msg:{@msg}", LogEvents.ExceptionHandled, http, auth, errorcode, Activity.Current?.Id ?? httpContext.TraceIdentifier, "Handled_Exception");
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

            AuthorizationException authexp => (
                (authexp.Status.Equals(AuthorizationFailureStatus.Unauthorized) ? ResultStatus.Unauthorized : ResultStatus.Forbidden),
                authexp.Code,
                authexp.Message,
                null
            ),

            IdempotencyException idemexp => (
               (idemexp.Status.Equals(IdempotencyFailureStatus.Conflict) ? ResultStatus.Conflict : ResultStatus.Invalid),
               idemexp.Code,
               idemexp.Message,
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
                 isDevelopment
                     ? GetFullExceptionMessage(ex)
                     : "An unexpected error occurred.",
                 null
             )
        };

        private static string GetFullExceptionMessage(Exception exception)
        {
            var messages = new List<string>();

            var current = exception;

            while (current is not null)
            {
                messages.Add(current.Message);
                current = current.InnerException;
            }

            return string.Join(" --> ", messages);
        }

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
