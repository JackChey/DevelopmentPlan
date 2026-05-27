using InprovePlan.IService.Prometheus;
using System.Text.Json;
using System.Web;

namespace InprovePlan.Service.Prometheus
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="_httpClient"></param>
    public class PrometheusQueryService(HttpClient _httpClient) : IPrometheusQueryService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<double?> QueryP50Async(CancellationToken ct = default)
        {
            // PromQL 说明：
            // 1) histogram_quantile(0.50, ...) 计算 P50
            // 2) rate(bucket[5m]) 使用最近 5 分钟窗口
            // 3) by (le) 是 histogram_quantile 的必要维度
            // 4) 乘以 1000 把秒转换为毫秒
            // 5) 排除 /metrics，避免被 Prometheus 自身抓取流量干扰
            var promQl = """
        1000 * histogram_quantile(
          0.50,
          sum(rate(microsoft_aspnetcore_hosting_http_server_request_duration_bucket{http_route!="/metrics"}[5m])) by (le)
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
        public async Task<double?> QueryP90Async(CancellationToken ct = default)
        {
            // PromQL 说明：
            // 1) histogram_quantile(0.50, ...) 计算 P90
            // 2) rate(bucket[5m]) 使用最近 5 分钟窗口
            // 3) by (le) 是 histogram_quantile 的必要维度
            // 4) 乘以 1000 把秒转换为毫秒
            // 5) 排除 /metrics，避免被 Prometheus 自身抓取流量干扰
            var promQl = """
        1000 * histogram_quantile(
          0.90,
          sum(rate(microsoft_aspnetcore_hosting_http_server_request_duration_bucket{http_route!="/metrics"}[5m])) by (le)
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
        public async Task<double?> QueryP95Async(CancellationToken ct = default)
        {
            // PromQL 说明：
            // 1) histogram_quantile(0.50, ...) 计算 P95
            // 2) rate(bucket[5m]) 使用最近 5 分钟窗口
            // 3) by (le) 是 histogram_quantile 的必要维度
            // 4) 乘以 1000 把秒转换为毫秒
            // 5) 排除 /metrics，避免被 Prometheus 自身抓取流量干扰
            var promQl = """
        1000 * histogram_quantile(
          0.95,
          sum(rate(microsoft_aspnetcore_hosting_http_server_request_duration_bucket{http_route!="/metrics"}[5m])) by (le)
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
        public async Task<double?> PrometheusQuery(string promQl, CancellationToken ct = default)
        {
            // Prometheus 查询接口：/api/v1/query?query=<promql>
            // 注意必须 URL 编码，避免空格/括号导致请求失败
            var url = "/api/v1/query?query=" + HttpUtility.UrlEncode(promQl);

            using var resp = await _httpClient.GetAsync(url, ct);

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

            var result = doc.RootElement.GetProperty("data").GetProperty("result");

            // 没有数据时返回 null（例如窗口内无请求）
            if (result.GetArrayLength() == 0) return null;

            // value[1] 是字符串数值
            var valueText = result[0].GetProperty("value")[1].GetString();

            // 解析失败时返回 null，避免抛格式异常
            return double.TryParse(valueText, out var ms) ? ms : null;
        }
    }
}
