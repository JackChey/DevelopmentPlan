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
            var loghttp = GetObj<LogHttpRequestInfo>(e, "http");

            if (loghttp is not null)
            {
                loghttp.DurationMs = double.Parse(GetString(e, "Elapsed") ?? "0");
            }

            return new AppExceptionLog()
            {
                OccurrenceTime = e.Timestamp,
                Level = e.Level.ToString(),
                Msg = GetString(e, "msg") ?? e.Exception?.Message,
                Event = GetString(e, "event") ?? string.Empty,
                Service = GetString(e, "service") ?? string.Empty,
                Env = GetString(e, "env") ?? string.Empty,
                Version = GetString(e, "version") ?? string.Empty,
                Instance = GetString(e, "instance") ?? string.Empty,
                TraceId = e.TraceId.ToString(),
                SpanId = e.SpanId.ToString(),
                Http = loghttp,
                Error = new LogErrorInfo()
                {
                    Code = GetString(e, "errorcode") ?? "code",
                    Message = e.Exception?.Message,
                    Stack = e.Exception?.StackTrace ,
                    Type = e.Exception?.GetType().ToString(),
                },
                Tags = GetStringArray(e, "tags"),
             
            };
        }
    }
}
