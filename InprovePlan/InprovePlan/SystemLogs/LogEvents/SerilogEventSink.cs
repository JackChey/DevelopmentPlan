using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace InprovePlan.SystemLogs.LogEvents
{
    /// <summary>
    /// 处理日志,日志往哪里写、怎么写
    /// </summary>
    public class SerilogEventSink() : ILogEventSink
    {
        /// <summary>
        /// 处理逻辑
        /// </summary>
        /// <param name="logEvent"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Emit(LogEvent logEvent)
        {
            // 获取日志信息

            var occurrenceTime = logEvent.Timestamp;
            var level = logEvent.Level.ToString();
            var msg = logEvent.Exception?.Message ?? "No Msg";
            logEvent.Properties.TryGetValue("instance", out var instance);
            logEvent.Properties.TryGetValue("service", out var service);
            logEvent.Properties.TryGetValue("version", out var version);
            logEvent.Properties.TryGetValue("env", out var env);
            logEvent.Properties.TryGetValue("event", out var logevent);
            logEvent.Properties.TryGetValue("http", out var http);

            var method = string.Empty;
            var route = string.Empty;
            var statuscode = 0;
            var clientip = string.Empty;

            if (http is StructureValue sv)
            {
                method = sv.Properties.FirstOrDefault(p => p.Name == "Method")?.Value.ToString();
                route = sv.Properties.FirstOrDefault(p => p.Name == "Route")?.Value.ToString();
                statuscode = string.IsNullOrEmpty(sv.Properties.FirstOrDefault(p => p.Name == "StatusCode")?.Value.ToString()) ? 0 :  int.Parse(sv.Properties.FirstOrDefault(p => p.Name == "StatusCode")!.Value!.ToString());
                clientip = sv.Properties.FirstOrDefault(p => p.Name == "ClientIp")?.Value.ToString();

            }

            var logHttp = new LogHttpRequestInfo()
            {
                ClientIp = clientip ?? "",
                Method = method ?? "",
                Route = route ?? "",
                StatusCode = statuscode,
            };

            var traceId = logEvent.TraceId;
            var spanId = logEvent.SpanId;

            if (logEvent.Level > LogEventLevel.Warning)
            {
                var expLog = new AppExceptionLog()
                {
                    OccurrenceTime = occurrenceTime,
                    Level = level,
                    Msg = msg,
                    Service = service?.ToString() ?? "No Service",
                    Env = env?.ToString() ?? "No Env",
                    Version = version?.ToString() ?? "No Version",
                    Instance = instance?.ToString() ?? "No Instance",
                    TraceId = traceId,
                    SpanId = spanId,
                    Event = logevent?.ToString() ?? "No Event",
                    Http = logHttp,
                };
            }
            else
            {
                var expLog = new AppLog()
                {
                    OccurrenceTime = occurrenceTime,
                    Level = level,
                    Msg = msg,
                    Service = service?.ToString() ?? "No Service",
                    Env = env?.ToString() ?? "No Env",
                    Version = version?.ToString() ?? "No Version",
                    Instance = instance?.ToString() ?? "No Instance",
                    TraceId = traceId,
                    SpanId = spanId,
                };
            }

            // 将日志信息序列号存储
        }
    }
}
