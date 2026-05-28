using InprovePlan.IService.Prometheus;
using InprovePlan.Prometheus;
using Microsoft.Extensions.Options;
using Prometheus;
using System.Globalization;
using System.Text.Json;
using System.Web;

namespace InprovePlan.Service.Prometheus
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="_httpClient"></param>
    /// <param name="_promSeetings"></param>
    public class PrometheusQueryService(HttpClient _httpClient,IOptions<PrometheusSettings> _promSeetings) : IPrometheusQueryService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<MetricQueryResult> QueryP50Async(CancellationToken ct = default)
        {
            // PromQL 说明：
            // 1) histogram_quantile(0.50, ...) 计算 P50
            // 2) rate(bucket[5m]) 使用最近 5 分钟窗口
            // 3) by (le) 是 histogram_quantile 的必要维度
            // 4) 乘以 1000 把秒转换为毫秒
            // 5) 排除 /metrics，避免被 Prometheus 自身抓取流量干扰
            var promQl = $$"""
1000 * histogram_quantile(
  0.50,
  sum(rate({{_promSeetings.Value.HttpDurationBucketMetric}}{http_route!="/metrics"}[5m])) by (le)
)
""";

            return await PrometheusQuery(promQl, ct);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<MetricQueryResult> QueryP90Async(CancellationToken ct = default)
        {
            // PromQL 说明：
            // 1) histogram_quantile(0.50, ...) 计算 P90
            // 2) rate(bucket[5m]) 使用最近 5 分钟窗口
            // 3) by (le) 是 histogram_quantile 的必要维度
            // 4) 乘以 1000 把秒转换为毫秒
            // 5) 排除 /metrics，避免被 Prometheus 自身抓取流量干扰
            var promQl = $$"""
1000 * histogram_quantile(
  0.90,
  sum(rate({{_promSeetings.Value.HttpDurationBucketMetric}}{http_route!="/metrics"}[5m])) by (le)
)
""";

            return await PrometheusQuery(promQl, ct);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<MetricQueryResult> QueryP95Async(CancellationToken ct = default)
        {
            // PromQL 说明：
            // 1) histogram_quantile(0.50, ...) 计算 P95
            // 2) rate(bucket[5m]) 使用最近 5 分钟窗口
            // 3) by (le) 是 histogram_quantile 的必要维度
            // 4) 乘以 1000 把秒转换为毫秒
            // 5) 排除 /metrics，避免被 Prometheus 自身抓取流量干扰
            var promQl = $$"""
1000 * histogram_quantile(
  0.95,
  sum(rate({{_promSeetings.Value.HttpDurationBucketMetric}}{http_route!="/metrics"}[5m])) by (le)
)
""";

            return await PrometheusQuery(promQl, ct);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="promQl"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<MetricQueryResult> PrometheusQuery(string promQl, CancellationToken ct = default)
        {
            try
            {
                // Prometheus 查询接口：/api/v1/query?query=<promql>
                // 注意必须 URL 编码，避免空格/括号导致请求失败
                var url = "/api/v1/query?query=" + HttpUtility.UrlEncode(promQl);

                using var resp = await _httpClient.GetAsync(url, ct);

                // 查询失败：HTTP 非 2xx
                if (!resp.IsSuccessStatusCode)
                {
                    return new MetricQueryResult(
                        Success: false,
                        HasData: false,
                        Value: null,
                        Reason: $"prometheus_http_error:{(int)resp.StatusCode}");
                }


                // 非 2xx 直接抛异常：上层可统一捕获并记录
                resp.EnsureSuccessStatusCode();

                // 解析 Prometheus JSON 返回
                // 典型结构：
                // {
                //   "status":"success",
                //   "data":{"resultType":"vector","result":[{"value":[timestamp,"123.45"]}]}
                // }
                using var stream = await resp.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                var root = doc.RootElement;

                // 查询失败：Prometheus 返回 status=error
                var status = root.GetProperty("status").GetString();
                if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
                {
                    var reason = root.TryGetProperty("error", out var err)
                        ? err.GetString()
                        : "prometheus_query_failed";
                    return new MetricQueryResult(false, false, null, reason);
                }

                var result = doc.RootElement.GetProperty("data").GetProperty("result");

                // 无数据窗口：result 为空
                if (result.GetArrayLength() == 0)
                {
                    return new MetricQueryResult(
                        Success: true,
                        HasData: false,
                        Value: null,
                        Reason: "insufficient_samples");
                }

                // value: [ unix_ts, "123.45" ]
                var valueText = result[0].GetProperty("value")[1].GetString();

                if (string.IsNullOrWhiteSpace(valueText))
                {
                    return new MetricQueryResult(true, false, null, "empty_value");
                }

                // Prometheus 可能返回 NaN / +Inf / -Inf
                if (string.Equals(valueText, "NaN", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(valueText, "+Inf", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(valueText, "-Inf", StringComparison.OrdinalIgnoreCase))
                {
                    return new MetricQueryResult(true, false, null, "insufficient_samples");
                }

                if (!double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    return new MetricQueryResult(false, false, null, "value_parse_failed");
                }

                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    return new MetricQueryResult(true, false, null, "insufficient_samples");
                }

                return new MetricQueryResult(true, true, value, null);
            }
            catch (OperationCanceledException)
            {
                return new MetricQueryResult(false, false, null, "request_canceled");
            }
            catch (Exception ex)
            {
                return new MetricQueryResult(false, false, null, $"exception:{ex.GetType().Name}");
            }
        }
    }
}
