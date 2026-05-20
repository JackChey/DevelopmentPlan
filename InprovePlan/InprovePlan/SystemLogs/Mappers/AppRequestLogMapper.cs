using Microsoft.AspNetCore.Http;
using Serilog.Events;
using System.Security.Claims;

namespace InprovePlan.SystemLogs.Mappers
{
    /// <summary>
    /// 
    /// </summary>
    public class AppRequestLogMapper(): LogMapper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static AppRequestLog Map(LogEvent e)
        {
            var loghttp = GetObj<LogHttpRequestInfo>(e, "http");

            if (loghttp is not null)
            {
                var temp = GetString(e, "Elapsed");

                loghttp.DurationMs = double.Parse(GetString(e, "Elapsed") ?? "0");
            }

            return new AppRequestLog()
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
                Auth = GetObj<LogAuthorizationInfo>(e, "auth"),
                Http = loghttp,
                Biz = GetObj<LogBusinessContext>(e, "biz"),
                Error = e.Exception?.Message ?? GetString(e, "error"),
                Tags = GetStringArray(e, "tags"),
             
            };
        }
    }
}
