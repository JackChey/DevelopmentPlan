using AutoMapper;
using InprovePlan.Connections;
using InprovePlan.Exceptions;
using InprovePlan.FakeData;
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
    [Route("api/[controller]")]
    public class HealthController(IOptions<DBConnection>? dbconection, IOptions<RabbitMqConnection>? mqconection, IOptions<RedisConnection>? redisconection) : BaseController
    {
        /// <summary>
        /// 服务存活检查
        /// </summary>
        /// <returns></returns>
        [HttpGet("live")]
        public IActionResult live()
        {
            return ReturnResult(Result.Seccess);
        }


        /// <summary>
        /// 服务就绪检查
        /// 包含:DataBase,Redis,下游服务等
        /// </summary>
        /// <returns></returns>
        [HttpGet("ready")]
        public IActionResult ready()
        {
            // 这里的检查只是演示代码,后续会补齐真实的验证逻辑

            // 检查DataBase
            if (string.IsNullOrEmpty(dbconection?.Value.server))
            {
                return ReturnResult(Result.Failure("数据库异常"));
            }

            // 检查Redis
            if (string.IsNullOrEmpty(redisconection?.Value.server))
            {
                return ReturnResult(Result.Failure("Redis异常"));
            }

            // 检查RabbitMq
            if (string.IsNullOrEmpty(mqconection?.Value.host))
            {
                return ReturnResult(Result.Failure("RabbitMq异常"));
            }

            return ReturnResult(Result.Seccess);
        }

    }
}
