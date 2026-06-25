using Instructure.Exceptions;
using Instructure.SystemLogs;
using Serilog;
using System.Diagnostics;

namespace InprovePlan.Middlewares
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RequestLifecycleMiddleware
    {
        /// <summary>
        /// 下一个中间件委托
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// 通过构造函数注入下一个中间件
        /// </summary>
        public RequestLifecycleMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// 每个 HTTP 请求都会进入此方法
        ///
        /// 执行流程：
        /// 1) 记录 started 事件
        /// 2) 调用后续中间件/控制器
        /// 3) 成功时记录 completed 事件
        /// 4) 异常时记录 failed 事件并继续抛出
        /// </summary>
        public async Task Invoke(HttpContext ctx)
        {
            // 使用 Stopwatch 统计“整条请求链路”耗时（毫秒）
            // 注意：这是从进入本中间件开始，到请求结束/异常结束为止
            var sw = Stopwatch.StartNew();

            // 获取请求信息
            var httpStart = new LogHttpRequestInfo()
            {
                Route = ctx.Request.Path,
                Method = ctx.Request.Method,
                StatusCode = ctx.Response.StatusCode,
                ClientIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "Unkown",
            };

            // -------- 1) 请求开始日志 --------
            // 这里仅记录基础请求信息，不记录状态码与耗时（尚未完成）
            Log.ForContext("event", LogEvents.HttpRequestStarted)
               .ForContext("http", httpStart, destructureObjects: true)
               .Information("http.request.started");

            // -------- 2) 执行后续管道 --------
            await _next(ctx);

            // 请求正常返回，停止计时
            sw.Stop();

            // 从 HttpContext.Items 获取鉴权信息（建议由前置中间件写入）
            // 若未写入则 auth 为 null，这属于可接受状态
            ctx.Items.TryGetValue("auth", out var auth);

            // 获取请求信息
            var httpEnd = new LogHttpRequestInfo()
            {
                Route = ctx.Request.Path,
                Method = ctx.Request.Method,
                StatusCode = ctx.Response.StatusCode,
                ClientIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "Unkown",
                DurationMs = sw.Elapsed.TotalMilliseconds,
            };

            // -------- 3) 请求完成日志 --------
            Log.ForContext("event", LogEvents.HttpRequestCompleted)
               .ForContext("auth", auth, destructureObjects: true)
               .ForContext("http", httpEnd, destructureObjects: true)
               .Information("http.request.completed");

            //try
            //{
                
            //}
            //catch (Exception ex)
            //{
            //    // 后续管道发生异常，停止计时
            //    sw.Stop();

            //    //// 获取请求信息
            //    //var httpFailed = new LogHttpRequestInfo()
            //    //{
            //    //    Route = ctx.Request.Path,
            //    //    Method = ctx.Request.Method,
            //    //    StatusCode = ctx.Response.StatusCode,
            //    //    ClientIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "Unkown",
            //    //    DurationMs = sw.Elapsed.TotalMilliseconds,
            //    //};

            //    //// -------- 4) 请求失败日志 --------
            //    //// 这里使用 Error，并附带异常对象，保留完整堆栈
            //    //// StatusCode 使用 500 作为默认失败状态（如果后续有统一异常处理中间件改写响应，不影响日志排障价值）
            //    //Log.ForContext("event", LogEvents.HttpRequestFailed)
            //    //   .ForContext("http", httpFailed, destructureObjects: true)
            //    //   .Error(ex, "http.request.failed");

            //    // 继续抛出异常，交给全局异常处理中间件处理响应
            //    //throw new Exception(LogEvents.HttpRequestFailed, ex);
            //}
        }

    }
}
