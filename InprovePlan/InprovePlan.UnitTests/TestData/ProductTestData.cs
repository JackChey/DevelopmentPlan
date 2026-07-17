using InprovePlan.Domain.Entities;
using Instructure.Configurations.Entities;

namespace InprovePlan.UnitTests.TestData;

/// <summary>
/// 商品测试数据常量类
/// </summary>
public static class ProductTestData
{
    /// <summary>
    /// 有效的商品ID
    /// </summary>
    public const long ValidProductId = 1;

    /// <summary>
    /// 无效的商品ID（通常用于测试边界条件或异常场景）
    /// </summary>
    public const long InvalidProductId = 0;

    /// <summary>
    /// 有效的商品编码
    /// </summary>
    public const string ValidProductCode = "UNIT-PRODUCT-001";

    /// <summary>
    /// 过长的商品编码（用于测试最大长度限制）
    /// </summary>
    public static readonly string TooLongProductCode = new('A', DataSchemaConstants.ProductCodeLength + 1);

    /// <summary>
    /// 有效的商品名称
    /// </summary>
    public const string ValidProductName = "unit_product_001";

    /// <summary>
    /// 有效的商品描述（符合长度要求）
    /// </summary>
    public const string ValidProductDescription = "符合长度要求的商品描述。";

    /// <summary>
    /// 有效的商品类型ID
    /// </summary>
    public const long ValidProductTypeId = 1;

    /// <summary>
    /// 有效的单价
    /// </summary>
    public const decimal ValidUnitPrice = 99.99m;

    /// <summary>
    /// 有效的货币代码
    /// </summary>
    public const string ValidCurrency = "CNY";

    /// <summary>
    /// 无效的货币代码（格式错误或不支持）
    /// </summary>
    public const string InvalidCurrency = "CN";

    /// <summary>
    /// 有效的商品搜索关键字
    /// </summary>
    public const string ValidKeyword = "product";

    /// <summary>
    /// 有效的商品状态（启用）
    /// </summary>
    public const AppProductStatus ValidProductStatus = AppProductStatus.Enable;

    /// <summary>
    /// 无效的商品状态（未定义的枚举值）
    /// </summary>
    public const AppProductStatus InvalidProductStatus = (AppProductStatus)999;
}

