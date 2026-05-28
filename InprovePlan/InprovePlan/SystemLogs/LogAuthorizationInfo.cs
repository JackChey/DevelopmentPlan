namespace InprovePlan.SystemLogs
{
    /// <summary>
    ///  http 请求用户信息,用于记录日志
    /// </summary>
    public class LogAuthorizationInfo
    {
        /// <summary>
        /// 是否授权
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// 请求用户ID
        /// </summary>
        public string? UserId { get; set; } = string.Empty;

        /// <summary>
        /// 用户名
        /// </summary>
        public string? UserName { get; set; } = string.Empty;

        /// <summary>
        /// 授权策略
        /// </summary>
        public string? AuthScheme { get; set; } = string.Empty;

        /// <summary>
        /// 角色
        /// </summary>
        public string[]? Roles { get; set; } 
       
    }
}
