using Microsoft.AspNetCore.Diagnostics;

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Instructure.IResult;

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
            var (status, title, type, errorCode) = Map(exception);

            // 若异常状态大于等于 500 ,则代表重大异常,需要进行记录,
            if (status >= 500)
            {
                _logger.LogError(exception, "Unhandled exception.TraceId={TraceId}",Activity.Current?.Id ?? httpContext.TraceIdentifier);
            }
            else
            {
                _logger.LogWarning(exception, "Handled bussiness exception.TraceId={TraceId}", Activity.Current?.Id ?? httpContext.TraceIdentifier);
            }

            var problem = new ProblemDetails()
            {
                Status = status,
                Title = title,
                Type = type,
                Detail = status >= 500 ? "An unexpected error occured.":exception.Message,
                Instance = httpContext.Request.Path,
            };

            problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            problem.Extensions["errorCode"] = errorCode;

            if (exception is ValidationException vex)
            {
                problem.Extensions["errors"] = vex.Errors;
            }

            httpContext.Response.StatusCode = status;
            await httpContext.Response.WriteAsJsonAsync( Result.From(new Result(ResultStatus.Ok )
            {

            })
            ,cancellationToken);
            await httpContext.Response.WriteAsJsonAsync(problem,cancellationToken);

            return true;
        }


        /// <summary>
        /// 拆分异常信息标准化
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        private static (int status,string title,string type,string errorCode) Map(System.Exception exception) => exception switch
        {
            ValidationException vex => (StatusCodes.Status400BadRequest,"Validation Failed", "https://httpstatuses.com/400", vex.ErrorCode),
            BusinessException bex => (StatusCodes.Status409Conflict,"Bussiness Rule Violation", "https://httpstatuses.com/409", bex.ErrorCode),
            NotFoundException nex => (StatusCodes.Status404NotFound,"Not Found", "https://httpstatuses.com/404", nex.ErrorCode),
            UnauthorizedAccessException =>(StatusCodes.Status403Forbidden,"Forbidden", "https://httpstatuses.com/403", "forbidden"),
            OperationCanceledException => (499,"Client Closed Request","about:blank","client_close_request"),
            _ => (StatusCodes.Status500InternalServerError,"Interal Server Error", "https://httpstatuses.com/500", "interal_error"),
        };
    }
}
