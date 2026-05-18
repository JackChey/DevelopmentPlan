using InprovePlan.SystemLogs;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace InprovePlan.Filters
{
    /// <summary>
    /// 
    /// </summary>
    public class AppActionFilter : IActionFilter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // 获取请求信息
            var http = new LogHttpRequestInfo()
            {
                Route = context.HttpContext.Request.Path,
                Method = context.HttpContext.Request.Method,
                StatusCode = context.HttpContext.Response.StatusCode,
                ClientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unkown",
            };

            Log.ForContext("http", http, destructureObjects: true)
                .ForContext("event", "http.request.completed").Information("http.request.completed");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // 获取请求信息
            var http = new LogHttpRequestInfo()
            {
                Route = context.HttpContext.Request.Path,
                Method = context.HttpContext.Request.Method,
                StatusCode = context.HttpContext.Response.StatusCode,
                ClientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unkown",
            };

            Log.ForContext("http", http, destructureObjects: true)
                .ForContext("event", "http.request.started").Information("http.request.started");
        }

       
    }
}
