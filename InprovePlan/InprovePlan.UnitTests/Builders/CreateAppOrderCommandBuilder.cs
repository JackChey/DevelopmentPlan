using InprovePlan.UnitTests.TestData;
using InprovePlan.UserCase.AppOrders.Commands;

namespace InprovePlan.UnitTests.Builders;

/// <summary>
/// 创建订单命令 (CreateAppOrderCommand) 的测试数据构建器。
/// 用于在单元测试中快速构造合法或特定场景下的命令对象，
/// 避免在每个测试方法中重复编写繁琐的对象初始化代码。
/// </summary>
public sealed class CreateAppOrderCommandBuilder
{
    // 默认商品ID：初始化为测试数据中定义的有效商品ID。
    // 这确保了在未显式指定ID时，构建出的命令包含一个合法的商品标识符。
    private long _productId = AppOrderTestData.ValidProductId;

    // 默认购买数量：初始化为测试数据中定义的有效数量（通常为正数）。
    // 这确保了在未显式指定数量时，构建出的命令符合基本的业务逻辑要求。
    private decimal _quantity = AppOrderTestData.ValidQuantity;

    // 默认收货地址ID：初始化为测试数据中定义的有效地址ID。
    // 这确保了在未显式指定地址时，构建出的命令包含一个合法的配送目标。
    private long _addressId = AppOrderTestData.ValidAddressId;

    /// <summary>
    /// 设置商品ID。
    /// </summary>
    /// <param name="productId">要设置的目标商品ID。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法允许测试人员自定义商品ID，例如测试无效商品ID或特定商品的下单逻辑。
    /// </remarks>
    public CreateAppOrderCommandBuilder WithProductId(long productId)
    {
        // 更新内部商品ID字段
        _productId = productId;

        // 返回 this 以支持链式调用，如: new Builder().WithProductId(...).Build()
        return this;
    }

    /// <summary>
    /// 设置购买数量。
    /// </summary>
    /// <param name="quantity">要设置的购买数量。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法允许测试人员自定义数量，例如测试零数量、负数数量或超大数量的边界情况。
    /// </remarks>
    public CreateAppOrderCommandBuilder WithQuantity(decimal quantity)
    {
        // 更新内部数量字段
        _quantity = quantity;

        // 返回 this 以支持链式调用
        return this;
    }

    /// <summary>
    /// 构建并返回最终的 CreateAppOrderCommand 对象。
    /// </summary>
    /// <returns>包含当前配置参数的命令对象实例。</returns>
    /// <remarks>
    /// 此方法是构建过程的终点。它使用当前内部存储的 _productId, _quantity 和 _addressId 
    /// 实例化命令对象。在此之后，构建器通常不再被使用，或者可以重置以构建新对象。
    /// </remarks>
    public CreateAppOrderCommand Build()
    {
        // 使用当前配置好的参数创建命令对象
        return new CreateAppOrderCommand(_productId, _quantity, _addressId);
    }
}

