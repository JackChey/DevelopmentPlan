using Instructure.Interfaces;

namespace InprovePlan.IntegrationTests.TestDoubles;

/// <summary>
/// 测试用当前用户。
///
/// 用于 AuthorizationBehavior 单元测试，
/// 避免依赖真实 HttpContext / ClaimsPrincipal。
/// </summary>
public sealed class FakeCurrentUser : IUser
{
    public long? Id { get; set; }
}