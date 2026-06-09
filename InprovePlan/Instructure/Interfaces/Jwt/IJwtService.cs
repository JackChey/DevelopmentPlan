using InprovePlan.Domain.Entities;
using Instructure.IResult;

namespace Instructure.Interfaces.Jwt
{
    /// <summary>
    /// 
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// 根据用户信息获取Jwt
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="passWord"></param>
        /// <returns></returns>
        //Task<Result<string>>  GetAccessTokenAsync(int userid,string passWord);
        string? GetAccessToken(AppUser appUser);
    }
}
