using AutoMapper;
using InprovePlan.Connections;
using InprovePlan.Exceptions;
using InprovePlan.FakeData;
using InprovePlan.IService.Prometheus;
using InprovePlan.ModeDto;
using InprovePlan.Model;
using InprovePlan.Service.Jwt;
using Instructure.IResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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

            return ReturnResult(new Result<double>(p95Ms ?? 0));
        }
    }
}
