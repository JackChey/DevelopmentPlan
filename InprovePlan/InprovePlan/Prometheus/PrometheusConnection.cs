namespace InprovePlan.Prometheus
{
    /// <summary>
    /// Prometheus 连接配置
    /// 包含:连接IP,端口
    /// </summary>
    public class PrometheusConnection
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
    }
}
