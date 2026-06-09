using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.IResult;

/// <summary>
/// 密码哈希服务接口。
///
/// 设计目标：
/// 1. 隔离具体密码哈希算法。
/// 2. Handler 不直接依赖 BCrypt、Argon2、PBKDF2 等具体实现。
/// 3. 方便未来替换算法。
/// 4. 方便单元测试中 Mock。
///
/// 注意：
/// 生产环境绝不能明文保存密码。
/// 数据库中只能保存 PasswordHash。
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// 对明文密码进行哈希。
    ///
    /// </summary>
    /// <param name="password">用户提交的明文密码。</param>
    /// <returns>可安全保存到数据库的密码哈希。</returns>
    string Hash(string password);

    /// <summary>
    /// 验证明文密码是否匹配已有密码哈希。
    ///
    /// 登录时使用。
    /// </summary>
    /// <param name="passwordHash">数据库中保存的密码哈希。</param>
    /// <param name="password">用户提交的明文密码。</param>
    /// <returns>密码校验结果。</returns>
    PasswordVerifyResult Verify(string passwordHash, string password);
}

/// <summary>
/// 密码验证结果。
///
/// 不直接暴露 Microsoft.AspNetCore.Identity 的枚举，
/// 避免应用层被具体组件强绑定。
/// </summary>
public enum PasswordVerifyResult
{
    /// <summary>
    /// 验证失败。
    /// </summary>
    Failed = 0,

    /// <summary>
    /// 验证成功。
    /// </summary>
    Success = 1,

    /// <summary>
    /// 验证成功，但当前哈希参数已经过旧，建议重新生成哈希并更新数据库。
    ///
    /// 例如未来提升 PBKDF2 迭代次数后，
    /// 老密码哈希验证成功时可能返回该状态。
    /// </summary>
    SuccessRehashNeeded = 2
}