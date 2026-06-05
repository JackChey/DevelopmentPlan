using Instructure.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace InprovePlan.Service
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    public class CurrentUser(IHttpContextAccessor httpContextAccessor) : IUser
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly ClaimsPrincipal? User = httpContextAccessor.HttpContext?.User;

        /// <summary>
        /// 
        /// </summary>
        public long? Id
        {
            get
            {
                if (User is null)
                {
                    return null;
                }

                return long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var result) ? result : null;
            }
        }
    }
}
