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
            return new AppRequestLog()
            {
                OccurrenceTime = e.Timestamp,
                Level = e.Level.ToString(),
                Msg = e.Exception?.Message,
                Event = GetString(e, "event") ?? string.Empty,
                Service = GetString(e, "service") ?? string.Empty,
                Env = GetString(e, "env") ?? string.Empty,
                Version = GetString(e, "version") ?? string.Empty,
                Instance = GetString(e, "instance") ?? string.Empty,
                TraceId = e.TraceId,
                SpanId = e.SpanId,
                Auth = GetObj<LogAuthorizationInfo>(e, "auth"),
                Http = GetObj<LogHttpRequestInfo>(e, "http") ,
                Biz = GetObj<LogBusinessContext>(e, "biz"),
                Error = e.Exception?.Message ?? GetString(e, "error"),
                Tags = GetStringArray(e, "tags"),
            };
        }
    }
}
