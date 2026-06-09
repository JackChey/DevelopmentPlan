using InprovePlan.Domain.Entities;
using Instructure.Interfaces.Jwt;
using Instructure.IResult;
using Instructure.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Instructure.Services.Jwt
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
        /// <param name="username"></param>
        /// <param name="passWord"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        //public async Task<Result<string>> GetAccessTokenAsync(int userId, string passWord)
        public string? GetAccessToken(AppUser appUser)
        {
            // 生成token
            var jwt = new JwtSecurityToken(
            jwtsettings.Value.Issuer,
            jwtsettings.Value.Audience,
            new[]
            {
                new Claim(ClaimTypes.Name,appUser.UserName),
                new Claim(ClaimTypes.NameIdentifier,appUser.Id.ToString()),
                new Claim(ClaimTypes.Role,"user"),
            },
            expires: DateTime.Now.AddMinutes(jwtsettings.Value.AccessTokenExpirationMinutes),
            signingCredentials: new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtsettings.Value.Secret)), SecurityAlgorithms.HmacSha256)
             );

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);

            return token;
        }
    }
}
