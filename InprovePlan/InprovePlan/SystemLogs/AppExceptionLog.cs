namespace InprovePlan.SystemLogs
{
    /// <summary>
    /// 异常日志
    /// </summary>
    public class AppExceptionLog: AppLog
    {

        /// <summary>
        /// 事件名（机器可读语义），如 http.request.completed，用于统一聚合统计
        /// </summary>
        public string Event { get; set; } = string.Empty;

        /// <summary>
        /// 请求方法信息
        /// </summary>
        public LogHttpRequestInfo? Http { get; set; } = null!;

        /// <summary>
        /// 错误信息
        /// </summary>
        public LogErrorInfo? Error { get; set; } = null!;

        /// <summary>
        /// 日志标签
        /// </summary>
        public string[]? Tags { get; set; } 

    }
}
