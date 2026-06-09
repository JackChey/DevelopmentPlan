using InprovePlan.Domain.Entities;
using Instructure.Interfaces.Jwt;

namespace InprovePlan.ApiTests.Infrastructure;

/// <summary>
/// API 测试用 JWT 服务。
///
/// 用固定字符串代替真实 JWT。
/// </summary>
public sealed class ApiTestJwtService : IJwtService
{
    public string GetAccessToken(AppUser appUser)
    {
        return $"api-test-token-{appUser.Id}";
    }
}