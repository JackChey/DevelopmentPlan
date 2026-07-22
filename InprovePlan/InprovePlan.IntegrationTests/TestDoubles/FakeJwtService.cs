using InprovePlan.Domain.Entities;
using Instructure.Interfaces.Jwt;

namespace InprovePlan.IntegrationTests.TestDoubles;

/// <summary>
/// 伪造的 JWT 服务实现，用于测试或开发环境。
/// 实现了 IJwtService 接口，提供简化的访问令牌获取逻辑，不进行真实的 JWT 生成或验证。
/// </summary>
internal sealed class FakeJwtService : IJwtService
{
    /// <summary>
    /// 模拟的访问令牌字符串。
    /// 默认值为 "test-access-token"，可根据测试需求进行修改。
    /// </summary>
    public string? AccessToken { get; set; } = "test-access-token";

    /// <summary>
    /// 获取指定应用用户的访问令牌。
    /// 在伪造实现中，忽略用户参数，直接返回预设的 AccessToken 属性值。
    /// </summary>
    /// <param name="appUser">应用用户对象，在此实现中未被使用。</param>
    /// <returns>预设的模拟访问令牌字符串。</returns>
    public string? GetAccessToken(AppUser appUser)
    {
        return AccessToken;
    }
}

