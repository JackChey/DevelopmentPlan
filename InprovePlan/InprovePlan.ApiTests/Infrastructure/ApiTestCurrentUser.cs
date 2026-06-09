using Instructure.Interfaces;

namespace InprovePlan.ApiTests.Infrastructure;

/// <summary>
/// API 测试用当前用户。
///
/// 用于模拟已登录用户。
/// 测试授权接口时，可以在测试中设置 Id。
/// </summary>
public sealed class ApiTestCurrentUser : IUser
{
    public long? Id { get; set; }
}