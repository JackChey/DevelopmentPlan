using Instructure.IResult;

namespace InprovePlan.IntegrationTests.TestDoubles;

/// <summary>
/// 伪造的密码哈希器实现，用于测试或开发环境。
/// 实现了 IPasswordHasher 接口，提供简化的密码哈希和验证逻辑，不使用真实的加密算法。
/// </summary>
internal class FakePasswordHasher : IPasswordHasher
{
    /// <summary>
    /// 对明文密码进行“哈希”处理。
    /// 在伪造实现中，仅简单地在密码前添加 "Hash:" 前缀，不进行任何实际的加密或加盐操作。
    /// </summary>
    /// <param name="password">需要哈希处理的明文密码。</param>
    /// <returns>模拟的哈希字符串，格式为 "Hash:{password}"。</returns>
    public string Hash(string password)
    {
        return $"Hash:{password}";
    }

    /// <summary>
    /// 验证提供的密码是否与存储的哈希值匹配。
    /// 通过重新计算输入密码的模拟哈希值，并与存储的哈希值进行字符串比较来完成验证。
    /// </summary>
    /// <param name="passwordHash">存储在数据库或系统中的哈希密码字符串。</param>
    /// <param name="password">用户输入的待验证明文密码。</param>
    /// <returns>如果密码匹配则返回 PasswordVerifyResult.Success，否则返回 PasswordVerifyResult.Failed。</returns>
    public PasswordVerifyResult Verify(string passwordHash, string password)
    {
        // 重新计算输入密码的模拟哈希值，并与存储的哈希值进行比较
        return passwordHash == Hash(password)
            ? PasswordVerifyResult.Success
            : PasswordVerifyResult.Failed;
    }
}

