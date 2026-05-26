using InprovePlan.SystemLogs;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using System.Diagnostics;

namespace InprovePlan.Filters
{
    /// <summary>
    /// 
    /// </summary>
    public class AppActionFilter : IActionFilter
    {
        // 用于在 HttpContext.Items 中存储 Stopwatch 的键
        private const string StopwatchKey = "__PerformanceStopwatch";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            double elapsedMs = 0;


            if (context.HttpContext.Items[StopwatchKey] is Stopwatch stopwatch)
            {
                // 停止计时
                stopwatch.Stop();

                // 获取耗时（毫秒）
                elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            }


            // 获取请求信息
            var http = new LogHttpRequestInfo()
            {
                Route = context.HttpContext.Request.Path,
                Method = context.HttpContext.Request.Method,
                StatusCode = context.HttpContext.Response.StatusCode,
                ClientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unkown",
                DurationMs = elapsedMs,
            };

            Log.ForContext("http", http, destructureObjects: true)
                .ForContext("event", "http.request.completed")
                .ForContext("msg", "http.request.completed")
                .Information("http.request.completed");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // 创建并启动计时器
            var stopwatch = Stopwatch.StartNew();

            // 将计时器存入当前请求的上下文中
            context.HttpContext.Items[StopwatchKey] = stopwatch;

            // 获取请求信息
            var http = new LogHttpRequestInfo()
            {
                Route = context.HttpContext.Request.Path,
                Method = context.HttpContext.Request.Method,
                StatusCode = context.HttpContext.Response.StatusCode,
                ClientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unkown",
            };

            Log.ForContext("http", http, destructureObjects: true)
                .ForContext("event", "http.request.started")
                .ForContext("msg", "http.request.started")
                .Information("http.request.started");
        }


    }
}
