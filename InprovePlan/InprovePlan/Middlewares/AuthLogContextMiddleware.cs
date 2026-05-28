
using InprovePlan.SystemLogs;
using System.Security.Claims;

namespace InprovePlan.Middlewares
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="_next"></param>
    public class AuthLogContextMiddleware(RequestDelegate _next) 
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            var user = context.User;

            var auth = new LogAuthorizationInfo
            {
                IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
                UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"),
                UserName = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.Name),
                Roles = user.FindAll(ClaimTypes.Role).Select(x => x.Value).Distinct().ToArray(),
                AuthScheme = user.Identity?.AuthenticationType
            };

            context.Items["auth"] = auth;

            await _next(context);
        }
    }
}
