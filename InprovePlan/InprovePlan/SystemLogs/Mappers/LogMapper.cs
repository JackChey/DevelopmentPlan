using Microsoft.AspNetCore.Http;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.Security.Claims;
using System.Text.Json;

namespace InprovePlan.SystemLogs.Mappers
{
    /// <summary>
    /// 
    /// </summary>
    public class LogMapper()
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        protected static string? GetString(LogEvent e, string key) =>
        e.Properties.TryGetValue(key, out var v) && v is ScalarValue s ? s.Value?.ToString() : null;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        protected static T? GetObj<T>(LogEvent e, string key)
        {
            if (!e.Properties.TryGetValue(key, out var value))
                return default;

            try
            {
                using var sw = new StringWriter();

                // typeTagName: null => 不输出 _type 之类额外字段
                var formatter = new JsonValueFormatter(typeTagName: null);
                formatter.Format(value, sw);

                var json = sw.ToString(); // 这里是标准 JSON
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        protected static string[]? GetStringArray(LogEvent e, string key)
        {
            if (!e.Properties.TryGetValue(key, out var v) || v is not SequenceValue seq) return null;
            return seq.Elements
                .OfType<ScalarValue>()
                .Select(x => x.Value?.ToString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .ToArray();
        }
    }
}
