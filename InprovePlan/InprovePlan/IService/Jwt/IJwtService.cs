using InprovePlan.Model;
using Instructure.IResult;

namespace InprovePlan.IService.Jwt
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
        Result<string> GetAccessToken(int userid,string passWord);
    }
}
