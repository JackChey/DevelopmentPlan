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
        /// P50查询
        /// </summary>
        /// <returns></returns>
        [HttpGet("P50")]
        public async Task<IActionResult> GetP50Async(CancellationToken ct)
        {
            var p50Ms = await _prometheus.QueryP50Async(ct);

            if (p50Ms is null )
            {
                return ReturnResult(Result.Failure("获取 Prometheus P50失败"));
            }

            if (p50Ms is double.NaN)
            {
                return ReturnResult(Result.Seccess("流量数据未达标,请稍等再试"));
            }

            return ReturnResult(new Result<double>(p50Ms ?? 0));
        }

        /// <summary>
        /// P90查询
        /// </summary>
        /// <returns></returns>
        [HttpGet("P90")]
        public async Task<IActionResult> GetP90Async(CancellationToken ct)
        {
            var p90Ms = await _prometheus.QueryP90Async(ct);

            if (p90Ms is null)
            {
                return ReturnResult(Result.Failure("获取 Prometheus P90失败"));
            }

            if (p90Ms is double.NaN)
            {
                return ReturnResult(Result.Seccess("流量数据未达标,请稍等再试"));
            }

            return ReturnResult(new Result<double>(p90Ms ?? 0));
        }


        /// <summary>
        /// P95查询
        /// </summary>
        /// <returns></returns>
        [HttpGet("P95")]
        public async Task<IActionResult> GetP95Async(CancellationToken ct)
        {
            var p95Ms = await _prometheus.QueryP95Async(ct);

            if (p95Ms is null)
            {
                return ReturnResult(Result.Failure("获取 Prometheus P95失败"));
            }

            if (p95Ms is double.NaN)
            {
                return ReturnResult(Result.Seccess("流量数据未达标,请稍等再试"));
            }

            return ReturnResult(new Result<double>(p95Ms ?? 0));
        }
    }
}
