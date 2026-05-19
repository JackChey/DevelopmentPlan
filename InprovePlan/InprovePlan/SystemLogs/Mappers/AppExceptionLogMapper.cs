using Microsoft.AspNetCore.Http;
using Serilog.Events;
using System.Security.Claims;

namespace InprovePlan.SystemLogs.Mappers
{
    /// <summary>
    /// 
    /// </summary>
    public class AppExceptionLogMapper(): LogMapper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static AppExceptionLog Map(LogEvent e)
        {
            return new AppExceptionLog()
            {
                OccurrenceTime = e.Timestamp,
                Level = e.Level.ToString(),
                Msg = e.Exception?.Message ,
                Event = GetString(e, "event") ?? string.Empty,
                Service = GetString(e, "service") ?? string.Empty,
                Env = GetString(e, "env") ?? string.Empty,
                Version = GetString(e, "version") ?? string.Empty,
                Instance = GetString(e, "instance") ?? string.Empty,
                TraceId = e.TraceId,
                SpanId = e.SpanId,
                Http = GetObj<LogHttpRequestInfo>(e, "http") ,
                Error = new LogErrorInfo()
                {
                    Code = GetString(e, "errorcode") ?? string.Empty,
                    Message = e.Exception?.Message,
                    Stack = e.Exception?.StackTrace ?? "Unkown Stack",
                    Type = e.Exception?.GetType().ToString(),
                },
                Tags = GetStringArray(e, "tags"),
            };
        }
    }
}
