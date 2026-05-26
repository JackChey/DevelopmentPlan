namespace InprovePlan.SystemLogs
{
    /// <summary>
    ///  http 请求信息,用于记录日志
    /// </summary>
    public class LogHttpRequestInfo
    {
        /// <summary>
        /// 请求类型
        /// </summary>
        public string? Method { get; set; } = string.Empty;

        /// <summary>
        /// 请求路由
        /// </summary>
        public string? Route { get; set; } = string.Empty;

        /// <summary>
        /// 响应状态
        /// </summary>
        public int? StatusCode { get; set; } 

        /// <summary>
        /// 耗时
        /// </summary>
        public double? DurationMs { get; set; }

        /// <summary>
        /// 客户端IP
        /// </summary>
        public string? ClientIp { get; set; } = string.Empty;


    }
}
