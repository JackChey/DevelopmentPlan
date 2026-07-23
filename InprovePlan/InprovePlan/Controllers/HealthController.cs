using InprovePlan.Connections;
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
    public class HealthController(IConfiguration configuration) : BaseController
    {
        /// <summary>
        /// 服务存活检查
        /// </summary>
        /// <returns></returns>
        [HttpGet("live")]
        [AllowAnonymous]
        public IActionResult live()
        {
            return ReturnResult(Result.SeccessWithNoMsg);
        }


        /// <summary>
        /// 服务就绪检查
        /// 包含:DataBase,Redis,下游服务等
        /// </summary>
        /// <returns></returns>
        [HttpGet("ready")]
        [AllowAnonymous]
        public IActionResult ready()
        {
            // 这里的检查只是演示代码,后续会补齐真实的验证逻辑

            // 检查DataBase
            if (string.IsNullOrEmpty(configuration.GetConnectionString("AppDbConnectionStrings")))
            {
                return ReturnResult(Result.Failure("数据库连接未配置"));
            }

            // 检查Redis
            if (string.IsNullOrEmpty(configuration.GetConnectionString("RedisConnection")))
            {
                return ReturnResult(Result.Failure("Redis连接未配置"));
            }

            // 检查RabbitMq
            if (string.IsNullOrEmpty(configuration.GetSection("RabbitMq")["Host"]))
            {
                return ReturnResult(Result.Failure("RabbitMq未配置"));
            }

            return ReturnResult(Result.SeccessWithNoMsg);
        }

    }
}
