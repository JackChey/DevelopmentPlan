using InprovePlan.SystemLogs.Mappers;
using Serilog.Events;
using Serilog.Formatting;
using System.Text.Json;

namespace InprovePlan.SystemLogs.Formatter
{
    /// <summary>
    /// 
    /// </summary>
    public class AppExceptionLogFormatter : ITextFormatter
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = null // 保持类属性名原样
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logEvent"></param>
        /// <param name="output"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            var model = AppExceptionLogMapper.Map(logEvent);
            output.WriteLine(JsonSerializer.Serialize(model, JsonOptions));
        }
    }
}
