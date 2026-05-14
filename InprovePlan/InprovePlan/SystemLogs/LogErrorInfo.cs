namespace InprovePlan.SystemLogs
{
    /// <summary>
    ///  日志异常信息 
    /// </summary>
    public class LogErrorInfo
    {
        /// <summary>
        /// 异常代码
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 异常类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 具体异常信息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 调用堆栈
        /// </summary>
        public string Stack { get; set; } = string.Empty;
    }
}
