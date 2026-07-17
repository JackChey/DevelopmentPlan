using InprovePlan.Domain.Entities;

namespace InprovePlan.UnitTests.TestData;

/// <summary>
/// 应用订单测试数据常量类
/// </summary>
public static class AppOrderTestData
{
    /// <summary>
    /// 有效的订单ID
    /// </summary>
    public const long ValidOrderId = 1;

    /// <summary>
    /// 有效的产品ID
    /// </summary>
    public const long ValidProductId = 1;

    /// <summary>
    /// 有效的用户ID
    /// </summary>
    public const long ValidUserId = 1;

    /// <summary>
    /// 有效的地址ID
    /// </summary>
    public const long ValidAddressId = 10001;

    /// <summary>
    /// 无效的ID（通常用于测试边界条件或异常场景）
    /// </summary>
    public const long InvalidId = 0;

    /// <summary>
    /// 无效的产品ID
    /// </summary>
    public const long InvalidProductId = 0;

    /// <summary>
    /// 无效的数量（零值）
    /// </summary>
    public const decimal InvalidQuantity = 0m;

    /// <summary>
    /// 无效的数量精度（超出允许的小数位数）
    /// </summary>
    public const decimal InvalidQuantityScale = 1.2345m;

    /// <summary>
    /// 有效的订单数量
    /// </summary>
    public const decimal ValidQuantity = 1.000m;

    /// <summary>
    /// 有效的订单搜索关键字
    /// </summary>
    public const string ValidOrderKeyword = "O20260611";

    /// <summary>
    /// 有效的订单状态（新增）
    /// </summary>
    public const AppOrderStatus ValidOrderStatus = AppOrderStatus.Addition;

    /// <summary>
    /// 变更后的订单状态（已支付）
    /// </summary>
    public const AppOrderStatus ChangedOrderStatus = AppOrderStatus.Paid;

    /// <summary>
    /// 无效的订单状态（未定义的枚举值）
    /// </summary>
    public const AppOrderStatus InvalidOrderStatus = (AppOrderStatus)999;
}

