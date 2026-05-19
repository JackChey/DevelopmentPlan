using InprovePlan.FakeData;
using InprovePlan.IService.Jwt;
using InprovePlan.Model;
using Instructure.IResult;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InprovePlan.Service.Jwt
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="jwtsettings"></param>
    public class JwtService(IOptions<JwtSettings> jwtsettings) : IJwtService
    {
        /// <summary>
        /// 获取Jwt
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="passWord"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<Result<string>> GetAccessTokenAsync(int userId, string passWord)
        {
            // 验证用户信息
            var appuser = Users._users.FirstOrDefault(x=>x.UserId.Equals(userId) && x.PassWord.Equals(passWord));

            if (appuser is null)
            {
                return Result<string>.Unauthorized(new string[] {"用户名或密码输入错误"});
            }

            // 生成token
            var jwt = new JwtSecurityToken(
            jwtsettings.Value.Issuer,
            jwtsettings.Value.Audience,
            new[]
            {
                new Claim(ClaimTypes.Name,appuser.UserName),
                new Claim(ClaimTypes.NameIdentifier,appuser.UserId.ToString()),
                new Claim(ClaimTypes.Role,appuser.Root!),
            },
            expires:System.DateTime.Now.AddMinutes(jwtsettings.Value.AccessTokenExpirationMinutes),
            signingCredentials: new( new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtsettings.Value.Secret)),SecurityAlgorithms.HmacSha256 )
             );

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);

            return token is null
            ? Result<string>.Failure()
            : Result<string>.Seccess(token);
        }
    }
}
