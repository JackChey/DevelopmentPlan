using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InprovePlan.Filters
{
    /// <summary>
    /// 处理授权异常
    /// </summary>
    public class AuthResponseOperationFilter : IOperationFilter
    {
        /// <summary>
        /// 处理逻辑
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="context"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // 获取执行方法
            var method = context.MethodInfo;

            // 判断是否有授权
            var hasAuthorize = method.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() == true || 
                method.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

            // 判断是否允许匿名
            var hasAllowAnonymous = method.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();

            // 若不需要授权或允许匿名则返回
            if (!hasAuthorize || hasAllowAnonymous) return;

            // 处理授权失败响应结果
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
        }
    }
}
