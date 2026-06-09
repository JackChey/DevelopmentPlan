using InprovePlan.Domain.Entities;
using Instructure.Interfaces.Jwt;

namespace InprovePlan.IntegrationTests.TestDoubles;

/// <summary>
/// 集成测试用 JWT 服务。
///
/// 用固定 token 代替真实 JWT。
/// </summary>
public sealed class FakeJwtService : IJwtService
{
    public string GetAccessToken(AppUser appUser)
    {
        return $"test-token-{appUser.Id}";
    }
}