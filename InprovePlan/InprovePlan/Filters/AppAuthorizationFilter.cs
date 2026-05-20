using Instructure.IResult;
using Instructure.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using System.Diagnostics;

namespace InprovePlan.Filters
{
    /// <summary>
    /// 
    /// </summary>
    public class AppAuthorizationFilter : IAuthorizationFilter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // 1. 获取当前请求的端点
            var endpoint = context.HttpContext.GetEndpoint();

            // 2. 检查端点元数据中是否包含 IAllowAnonymous
            // GetMetadata<T> 如果找到对应的元数据则返回实例，否则返回 null
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
            {
                // 如果标记了 [AllowAnonymous]，则跳过自定义授权逻辑，直接返回
                return;
            }

            var user = context.HttpContext.User;

            var traceId = context.HttpContext.TraceIdentifier;

            // 处理未授权
            if (!(user.Identity?.IsAuthenticated ?? true))
            {
                //  var response = ApiResponse<object>.Fail(
                //StatusCodes.Status401Unauthorized.ToString(),
                //"Unauthorized",
                //traceId);

                //context.Result = new UnauthorizedResult();
                Log.ForContext("event", "auth.access.unauthorized")
                   .ForContext("errorcode", "AUTH_UNAUTHORIZED")
                   .ForContext("msg", "unauthorized")
                   .Warning("auth.access.unauthorized");

                return;
            }

            // 处理权限不够(这里缺少仓储层后续完善
            // 去数据库校对 user 中的 Role,若权限不足则输出日志


            //throw new NotImplementedException();
        }
    }
}
