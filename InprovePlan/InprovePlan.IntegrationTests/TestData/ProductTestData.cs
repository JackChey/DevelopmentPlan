using InprovePlan.Domain.Entities;

namespace InprovePlan.IntegrationTests.TestData;

/// <summary>
/// 产品测试数据常量类。
/// 此类定义了用于单元测试和集成测试的标准化产品数据常量，确保测试用例之间数据的一致性和可维护性。
/// 
/// 主要用途：
/// 1. 为 Product 实体及其相关 DTO（如创建、更新命令）提供有效的默认值。
/// 2. 在 Arrange 阶段快速构建测试对象，避免硬编码魔法数字或字符串。
/// 3. 作为断言阶段的预期值参考，提高测试代码的可读性。
/// 
/// 注意：
/// - 所有字段均为 const，编译时确定，性能最优。
/// - 数据类型与领域模型（Domain Model）中的定义严格匹配。
/// - ValidProductDescription 特意构造为符合配置长度要求的文本，用于验证数据完整性约束。
/// </summary>
public class ProductTestData
{
    /// <summary>
    /// 有效的产品 ID。
    /// 用于模拟已持久化的产品主键，通常用于查询、更新或删除操作测试。
    /// </summary>
    public const long ValidProductId = 1001234567;

    /// <summary>
    /// 有效的产品代码。
    /// 业务层面的唯一标识符，通常用于外部展示、检索或接口交互。
    /// </summary>
    public const string ValidProductCode = "Code123456";

    /// <summary>
    /// 有效的产品名称。
    /// 用于测试名称字段的显示、存储及长度校验逻辑。
    /// </summary>
    public const string ValidProductName = "Production001";

    /// <summary>
    /// 有效的产品描述。
    /// 该字符串经过精心设计，符合 ProductConfiguration 中定义的长度限制，
    /// 用于验证描述字段的持久化和读取逻辑，避免因长度超限导致的测试失败。
    /// </summary>
    public const string ValidProductDescription = "符合 ProductConfiguration 长度要求的商品描述。";

    /// <summary>
    /// 有效的产品类型 ID。
    /// 指向产品分类或类型的主键，用于验证产品与类型之间的关联关系。
    /// </summary>
    public const long ValidProductTypeId = 100123456;

    /// <summary>
    /// 有效的产品状态枚举值。
    /// 定义为 Enable（启用），用于测试正常在售产品的业务逻辑分支。
    /// </summary>
    public const AppProductStatus ValidProductStatus = AppProductStatus.Enable;

    /// <summary>
    /// 有效的商品单价。
    /// 值为 19.8m，用于测试价格字段的精度处理、格式化及计算逻辑。
    /// </summary>
    public const decimal ValidUnitPrice = 19.8m;

    /// <summary>
    /// 有效的货币类型。
    /// 定义为 "RMB"，用于测试货币字段的格式化和校验逻辑。
    /// </summary>
    public const string ValidCurrency = "RMB";
}

