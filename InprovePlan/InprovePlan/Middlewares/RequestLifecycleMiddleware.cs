using InprovePlan.SystemLogs;
using Serilog;
using System.Diagnostics;

namespace InprovePlan.Middlewares
{
    /// <summary>
    /// 日志事件名称字典（固定事件名中心）
    ///
    /// 设计目的：
    /// 1) 统一 event 字段取值，避免手写字符串导致拼写不一致。
    /// 2) 让 Prometheus/ELK/Seq/Grafana 查询口径稳定。
    /// 3) 便于跨团队协作，日志可读性和可维护性更好。
    ///
    /// 命名规范建议：
    /// - 全小写
    /// - 点分层（domain.object.action）
    /// - 语义明确、尽量短
    /// - 结果态优先（started/completed/failed）
    ///
    /// 例如：
    /// - http.request.started
    /// - auth.access.unauthorized
    /// - biz.validation.failed
    /// - infra.db.failed
    /// </summary>
    public static class LogEvents
    {
        // =========================
        // HTTP 请求生命周期事件
        // =========================

        /// <summary>
        /// 请求开始（进入请求管道）
        /// 常用于统计入口流量与链路起点
        /// </summary>
        public const string HttpRequestStarted = "http.request.started";

        /// <summary>
        /// 请求完成（正常返回）
        /// 常用于统计成功请求、耗时分位数、接口 SLA
        /// </summary>
        public const string HttpRequestCompleted = "http.request.completed";

        /// <summary>
        /// 请求失败（发生未处理异常或明确失败）
        /// 常用于错误率统计和异常追踪
        /// </summary>
        public const string HttpRequestFailed = "http.request.failed";


        // =========================
        // 鉴权/授权事件
        // =========================

        /// <summary>
        /// 未认证（通常对应 401）
        /// 场景：缺 token、token 过期、签名不合法
        /// </summary>
        public const string AuthUnauthorized = "auth.access.unauthorized";

        /// <summary>
        /// 已认证但无权限（通常对应 403）
        /// 场景：角色不足、策略不通过
        /// </summary>
        public const string AuthForbidden = "auth.access.forbidden";


        // =========================
        // 业务层（可预期失败）事件
        // =========================

        /// <summary>
        /// 业务校验失败（参数合法但不满足业务规则）
        /// 场景：用户名违规、库存不足、状态不允许
        /// </summary>
        public const string BizValidationFailed = "biz.validation.failed";

        /// <summary>
        /// 业务冲突（常映射 409）
        /// 场景：重复提交、并发版本冲突
        /// </summary>
        public const string BizConflict = "biz.conflict";

        // =========================
        // 基础设施层事件
        // =========================

        /// <summary>
        /// 数据库调用失败
        /// 场景：连接失败、超时、SQL 异常
        /// </summary>
        public const string InfraDbFailed = "infra.db.failed";

        /// <summary>
        /// 外部 HTTP 依赖失败
        /// 场景：下游服务 5xx、超时、网络异常
        /// </summary>
        public const string InfraHttpFailed = "infra.http.failed";

        /// <summary>
        /// 系统抛出异常但是被处理
        /// </summary>
        public const string ExceptionHandled = "exception.handled";

        /// <summary>
        /// 系统抛出异常但是未被处理
        /// </summary>
        public const string ExceptionUnhandled = "exception.unhandled";
    }

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
