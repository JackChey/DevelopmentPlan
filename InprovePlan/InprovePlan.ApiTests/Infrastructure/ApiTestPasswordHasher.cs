using Instructure.IResult;

namespace InprovePlan.ApiTests.Infrastructure;

/// <summary>
/// API 测试用密码哈希器。
///
/// 使用确定性 Hash，便于测试断言。
/// 生产环境不能使用该实现。
/// </summary>
public sealed class ApiTestPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return $"HASH::{password}";
    }

    public PasswordVerifyResult Verify(string passwordHash, string password)
    {
        return passwordHash == Hash(password)
            ? PasswordVerifyResult.Success
            : PasswordVerifyResult.Failed;
    }
}