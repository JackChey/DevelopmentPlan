namespace InprovePlan.SystemLogs
{
    /// <summary>
    ///  http 请求用户信息,用于记录日志
    /// </summary>
    public class LogAuthorizationInfo
    {
        /// <summary>
        /// 请求用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 租户ID
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// 角色
        /// </summary>
        public string[] Role { get; set; } = null!;
       
    }
}
