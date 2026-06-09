using InprovePlan.IService.Prometheus;
using Instructure.IResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InprovePlan.Controllers
{
    /// <summary>
    /// 用户业务接口
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class MetricsController(IPrometheusQueryService _prometheus) : BaseController
    {
        /// <summary>
        /// P50查询,返回以 ms 为单位
        /// </summary>
        /// <returns></returns>
        [HttpGet("P50")]
        public async Task<IActionResult> GetP50Async(CancellationToken ct)
        {
            var p50 = await _prometheus.QueryP50Async(ct);

            // 查询失败：Prometheus 不可用、语法错误、网络异常等
            if (!p50.Success)
            {
                return ReturnResult(Result.Failure($"查询失败: {p50.Reason}"));
            }

            // 无数据窗口：流量太低/窗口内无样本
            if (!p50.HasData || p50.Value is null)
            {
                return ReturnResult(Result.Success("当前时间窗口样本不足，请稍后重试"));
            }

            return ReturnResult(new Result<MetricQueryResult>(p50));
        }

        /// <summary>
        /// P90查询,返回以 ms 为单位
        /// </summary>
        /// <returns></returns>
        [HttpGet("P90")]
        public async Task<IActionResult> GetP90Async(CancellationToken ct)
        {
            var p90 = await _prometheus.QueryP90Async(ct);

            // 查询失败：Prometheus 不可用、语法错误、网络异常等
            if (!p90.Success)
            {
                return ReturnResult(Result.Failure($"查询失败: {p90.Reason}"));
            }

            // 无数据窗口：流量太低/窗口内无样本
            if (!p90.HasData || p90.Value is null)
            {
                return ReturnResult(Result.Success("当前时间窗口样本不足，请稍后重试"));
            }

            return ReturnResult(new Result<MetricQueryResult>(p90));
        }


        /// <summary>
        /// P95查询,返回以 ms 为单位
        /// </summary>
        /// <returns></returns>
        [HttpGet("P95")]
        public async Task<IActionResult> GetP95Async(CancellationToken ct)
        {
            var p95 = await _prometheus.QueryP95Async(ct);

            // 查询失败：Prometheus 不可用、语法错误、网络异常等
            if (!p95.Success)
            {
                return ReturnResult(Result.Failure($"查询失败: {p95.Reason}"));
            }

            // 无数据窗口：流量太低/窗口内无样本
            if (!p95.HasData || p95.Value is null)
            {
                return ReturnResult(Result.Success("当前时间窗口样本不足，请稍后重试"));
            }

            return ReturnResult(new Result<MetricQueryResult>(p95));
        }
    }
}
