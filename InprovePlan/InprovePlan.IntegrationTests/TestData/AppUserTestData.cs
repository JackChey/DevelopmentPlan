using InprovePlan.Domain.Entities;

namespace InprovePlan.IntegrationTests.TestData;

/// <summary>
/// 用户测试数据常量类。
/// 此类定义了用于单元测试和集成测试的标准化用户数据常量，确保测试用例之间数据的一致性和可维护性。
/// 
/// 主要用途：
/// 1. 为 AppUser 实体及其相关 DTO（如注册、登录、更新命令）提供有效的默认值。
/// 2. 在 Arrange 阶段快速构建测试对象，避免硬编码魔法数字或字符串。
/// 3. 作为断言阶段的预期值参考，提高测试代码的可读性。
/// 
/// 注意：
/// - 所有字段均为 const，编译时确定，性能最优。
/// - 数据类型与领域模型（Domain Model）中的定义严格匹配。
/// - 敏感信息（如密码、邮箱、手机号）仅用于测试环境，严禁在生产代码中使用真实用户数据。
/// </summary>
public class AppUserTestData
{
    /// <summary>
    /// 有效的用户 ID。
    /// 用于模拟已持久化的用户主键，通常用于查询、更新或删除操作测试。
    /// </summary>
    public const long ValidUserId = 100001;

    /// <summary>
    /// 有效的用户名。
    /// 用于测试用户名称的显示、存储及唯一性校验逻辑。
    /// </summary>
    public const string ValidUserName = "Jack";

    /// <summary>
    /// 有效的电子邮件地址。
    /// 用于测试邮箱格式校验、唯一性约束及通知发送逻辑。
    /// </summary>
    public const string ValidEmail = "18273940218@163.com";

    /// <summary>
    /// 有效的手机号码。
    /// 用于测试手机号格式校验、唯一性约束及短信验证逻辑。
    /// </summary>
    public const string ValidPhoneNumber = "18273940218";

    /// <summary>
    /// 有效的明文密码。
    /// 用于测试密码哈希、强度校验及登录验证逻辑。
    /// 注意：在实际存储前必须经过哈希处理，此处仅作为测试输入值。
    /// </summary>
    public const string ValidPassword = "123456";

    /// <summary>
    /// 有效的性别枚举值。
    /// 定义为 Secret（保密），用于测试性别字段的默认值或隐私保护逻辑。
    /// </summary>
    public const AppUserSex ValidSex = AppUserSex.Secret;

    /// <summary>
    /// 有效的用户状态枚举值。
    /// 定义为 Enable（启用），用于测试正常活跃用户的业务逻辑分支。
    /// </summary>
    public const AppUserStatus ValidUserStatus = AppUserStatus.Enable;

    /// <summary>
    /// 有效的删除标记。
    /// 定义为 false，表示用户未被逻辑删除，用于测试正常流程下的用户数据处理。
    /// </summary>
    public const bool ValidIsDeleted = false;
}

