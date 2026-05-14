namespace InprovePlan.SystemLogs
{
    /// <summary>
    ///  请求业务逻辑信息
    /// </summary>
    public class LogBusinessContext
    {
        /// <summary>
        /// 请求业务模块
        /// </summary>
        public string Module { get; set; } = string.Empty;

        /// <summary>
        /// 请求操作类型
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 业务主键（订单号等）
        /// </summary>
        public string BizId { get; set; } = string.Empty;
       
    }
}
