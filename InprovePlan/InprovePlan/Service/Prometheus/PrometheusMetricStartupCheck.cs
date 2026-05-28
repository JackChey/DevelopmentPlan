
using InprovePlan.Prometheus;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using System.Web;

namespace InprovePlan.Service.Prometheus
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="_httpClientFactory"></param>
    /// <param name="_promSeetings"></param>
    /// <param name="_logger"></param>
    public class PrometheusMetricStartupCheck(
        IHttpClientFactory _httpClientFactory,
        IOptions<PrometheusSettings> _promSeetings,
        ILogger<PrometheusMetricStartupCheck> _logger) : IHostedService
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var opt = _promSeetings.Value;

            // 创建独立 HttpClient（通过工厂创建，避免 socket 资源问题）
            var client = _httpClientFactory.CreateClient("prom-check");
            client.BaseAddress = new Uri($"http://{opt.IP}:{opt.Port}");
            client.Timeout = TimeSpan.FromSeconds(5);

            // 使用 /api/v1/series 检查“某指标名是否有时间序列”
            // 注意：match[] 需要 URL 编码
            var metric = Uri.EscapeDataString(opt.HttpDurationBucketMetric);
            var url = $"/api/v1/series?match[]={metric}";

            try
            {
                using var resp = await client.GetAsync(url, cancellationToken);
                resp.EnsureSuccessStatusCode();

                // Prometheus 返回结构：
                // { "status":"success", "data":[ {...}, {...} ] }
                using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                // data 数组长度 > 0 说明存在该指标对应序列
                var data = doc.RootElement.GetProperty("data");
                var exists = data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0;

                if (!exists)
                {
                    // 严格模式：直接抛异常阻止应用继续启动
                    var msg = $"Prometheus metric not found: {opt.HttpDurationBucketMetric}";
                    if (opt.FailFastOnMetricMissing) throw new InvalidOperationException(msg);

                    // 宽松模式：仅记录错误，服务继续启动
                    _logger.LogError(msg);
                }
                else
                {
                    _logger.LogInformation("Prometheus metric check passed: {Metric}", opt.HttpDurationBucketMetric);
                }
            }
            catch (Exception ex)
            {
                // Prometheus 不可达、JSON 解析失败、超时等都会走到这里
                // 根据配置决定是否中止启动
                if (opt.FailFastOnMetricMissing) throw;
                _logger.LogError(ex, "Prometheus startup metric check failed.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
