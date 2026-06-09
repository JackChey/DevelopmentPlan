using Instructure.IResult;

namespace InprovePlan.IntegrationTests.TestDoubles;

/// <summary>
/// 集成测试用密码哈希器。
///
/// 用确定性字符串替代真实哈希，
/// 方便断言密码是否被修改。
/// </summary>
public sealed class FakePasswordHasher : IPasswordHasher
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