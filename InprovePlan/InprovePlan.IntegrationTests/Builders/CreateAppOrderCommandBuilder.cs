using InprovePlan.IntegrationTests.TestData;
using InprovePlan.UserCase.AppOrders.Commands;

namespace InprovePlan.IntegrationTests.Builders;

/// <summary>
/// 创建应用订单命令构建器，用于创建和配置 CreateAppOrderCommand 实例。
/// 采用建造者模式，支持链式调用以灵活设置下单所需的参数（商品ID、数量、地址ID）。
/// 主要用于测试场景中快速构造不同的命令对象。
/// </summary>
public class CreateAppOrderCommandBuilder
{
    /// <summary>
    /// 商品 ID，初始化为测试数据中的有效商品 ID。
    /// </summary>
    private long _productId = AppOrderTestData.ValidProductId;

    /// <summary>
    /// 购买数量，初始化为测试数据中的有效数量。
    /// </summary>
    private decimal _quantity = AppOrderTestData.ValidQuantity;

    /// <summary>
    /// 收货地址 ID，初始化为测试数据中的有效地址 ID。
    /// </summary>
    private long _addressId = AppOrderTestData.ValidAddressId;

    /// <summary>
    /// 设置商品 ID。
    /// </summary>
    /// <param name="productId">要设置的目标商品 ID。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法允许测试人员自定义商品 ID，例如测试无效商品 ID 或特定商品的下单逻辑。
    /// </remarks>
    public CreateAppOrderCommandBuilder WithProductId(long productId)
    {
        // 更新内部商品 ID 字段
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
    /// 设置收货地址 ID。
    /// </summary>
    /// <param name="addressId">要设置的收货地址 ID。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法允许测试人员自定义地址 ID，例如测试无效地址或特定地址的配送逻辑。
    /// </remarks>
    public CreateAppOrderCommandBuilder WithAddressId(long addressId)
    {
        // 更新内部地址 ID 字段
        _addressId = addressId;

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

