using Serilog.Configuration;
using Serilog.Formatting;
using Serilog;
using InprovePlan.SystemLogs.LogEvents;

namespace InprovePlan.Extension
{
    /// <summary>
    /// 
    /// </summary>
    public static class LoggerConfigurationExt
    {
        /// <summary>
        /// 扩展方法：配置按级别分离的文件日志
        /// </summary>
        /// <param name="sinkConfiguration">Serilog Sink 配置上下文</param>
        /// <param name="highLevelPath">高优先级日志路径</param>
        /// <param name="lowLevelPath">低优先级日志路径</param>
        /// <returns>LoggerConfiguration 实例，支持链式调用</returns>
        public static LoggerConfiguration WriteToLevelSeparatedFile(
            this LoggerSinkConfiguration sinkConfiguration,
            string highLevelPath,
            string lowLevelPath)
        {
            // 实例化自定义 Sink 并注册到 Serilog 管道中
            return sinkConfiguration.Sink(new LevelSeparatingSink(highLevelPath, lowLevelPath));
        }
    }
}
