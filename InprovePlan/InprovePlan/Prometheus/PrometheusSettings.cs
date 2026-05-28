namespace InprovePlan.Prometheus
{
    /// <summary>
    /// Prometheus 连接配置
    /// 包含:连接IP,端口
    /// </summary>
    public class PrometheusSettings
    {
        /// <summary>
        /// 连接IP
        /// </summary>
        public string IP {  get; set; } = string.Empty;

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }   

        /// <summary>
        /// 响应超时
        /// </summary>
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// 响应指标
        /// </summary>
        public string HttpDurationBucketMetric { get; set; } = string.Empty;

        /// <summary>
        /// 是否在指标缺失时“启动失败”
        /// true: 抛异常终止启动（严格模式）
        /// false: 仅记录错误日志（宽松模式，推荐先用）
        /// </summary>
        public bool FailFastOnMetricMissing { get; set; } = false;
    }
}
