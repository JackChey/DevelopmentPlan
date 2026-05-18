using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.IO;
using System.Text;
using InprovePlan.Exceptions;

namespace InprovePlan.SystemLogs.LogEvents
{
    /// <summary>
    /// 处理日志,日志往哪里写、怎么写
    /// </summary>
    public class SerilogEventSink() : ILogEventSink
    {
        /// <summary>
        /// 
        /// </summary>
        public static readonly object _lock = new object();

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
                statuscode = string.IsNullOrEmpty(sv.Properties.FirstOrDefault(p => p.Name == "StatusCode")?.Value.ToString()) ? 0 : int.Parse(sv.Properties.FirstOrDefault(p => p.Name == "StatusCode")!.Value!.ToString());
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

                // 将日志信息序列号存储
                WriteJsonFile(expLog);
            }
            else
            {
                var applog = new AppLog()
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

                // 将日志信息序列号存储
                WriteJsonFile(applog);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        private void WriteJsonFile(AppExceptionLog appException)
        {
            var line = JsonSerializer.Serialize(appException);

            var filePath = Path.Combine(AppContext.BaseDirectory, "AppLog.ndjson");

            lock (_lock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.AppendAllText(filePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void WriteJsonFile(AppLog appLog)
        {
            var line = JsonSerializer.Serialize(appLog);

            var filePath = Path.Combine(AppContext.BaseDirectory + "/Logs", "AppLogs.ndjson");

            lock (_lock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.AppendAllText(filePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
    }
}
