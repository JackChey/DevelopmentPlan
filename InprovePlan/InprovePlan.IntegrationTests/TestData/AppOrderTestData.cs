using InprovePlan.Domain.Entities;

namespace InprovePlan.IntegrationTests.TestData;

/// <summary>
/// 订单测试数据常量类。
/// 此类定义了用于单元测试和集成测试的标准化订单数据常量，确保测试用例之间数据的一致性和可维护性。
/// 
/// 主要用途：
/// 1. 为 Order 实体及其相关 DTO 提供有效的默认值。
/// 2. 在 Arrange 阶段快速构建测试对象，避免硬编码魔法数字或字符串。
/// 3. 作为断言阶段的预期值参考，提高测试代码的可读性。
/// 
/// 注意：
/// - 所有字段均为 const，编译时确定，性能最优。
/// - 数据类型与领域模型（Domain Model）中的定义严格匹配。
/// </summary>
public class AppOrderTestData
{
    /// <summary>
    /// 有效的订单 ID。
    /// 用于模拟已持久化的订单主键，通常用于查询或更新操作测试。
    /// </summary>
    public const long ValidOrderId = 100234567;

    /// <summary>
    /// 有效的订单编号。
    /// 业务层面的唯一标识符，通常用于外部展示或接口交互。
    /// </summary>
    public const string ValidOrderNo = "NO123456789";

    /// <summary>
    /// 有效的关联产品 ID。
    /// 指向订单中某个具体产品的标识，用于验证订单项与产品的关联关系。
    /// </summary>
    public const long ValidProductId = 100001;

    /// <summary>
    /// 有效的关联产品代码。
    /// 用于验证订单中产品代码的正确性，通常与 ProductCode 字段对应。
    /// </summary>
    public const string ValidProductCode = "ProductCode1001";

    /// <summary>
    /// 有效的关联产品名称。
    /// 用于验证订单中产品名称的显示或存储逻辑。
    /// </summary>
    public const string ValidProductName = "Production001";

    /// <summary>
    /// 有效的货币类型。
    /// 定义为 "RMB"，用于测试货币字段的格式化和校验逻辑。
    /// </summary>
    public const string ValidCurrency = "RMB";

    /// <summary>
    /// 有效的商品单价。
    /// 值为 19.8m，用于测试价格计算、精度处理及总金额推导逻辑。
    /// </summary>
    public const decimal ValidUnitPrice = 19.8m;

    /// <summary>
    /// 有效的购买数量。
    /// 值为 10，用于测试数量校验及总价计算（单价 * 数量）。
    /// </summary>
    public const decimal ValidQuantity = 10;

    /// <summary>
    /// 有效的订单状态。
    /// 定义为 Paid（已支付），用于测试状态流转、权限控制或业务规则分支。
    /// </summary>
    public const AppOrderStatus ValidOrderStatus = AppOrderStatus.Paid;

    /// <summary>
    /// 有效的取消标记。
    /// 定义为 false，表示订单未被取消，用于测试正常流程下的订单处理。
    /// </summary>
    public const bool ValidCancelled = false;

    /// <summary>
    /// 有效的收货地址 ID。
    /// 指向订单关联的收货地址记录，用于测试地址信息的加载和验证。
    /// </summary>
    public const long ValidAddressId = 100001;
}

