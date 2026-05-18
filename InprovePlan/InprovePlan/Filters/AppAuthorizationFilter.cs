using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

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
            var user = context.HttpContext.User;

            // 处理未授权
            if (!(user.Identity?.IsAuthenticated ?? true))
            {
                context.Result = new UnauthorizedResult();
                Log.ForContext("event", "auth.access.unauthorized").Warning("");

                return;
            }

            // 处理全选不够

            throw new NotImplementedException();
        }
    }
}
