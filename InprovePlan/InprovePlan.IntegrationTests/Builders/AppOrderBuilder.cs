using InprovePlan.Domain.Entities;
using InprovePlan.IntegrationTests.TestData;
using Instructure.Interfaces;

namespace InprovePlan.IntegrationTests.Builders;

/// <summary>
/// 应用订单构建器，用于创建和配置 AppOrder 实例。
/// 采用建造者模式，支持链式调用以灵活设置订单属性。
/// </summary>
public class AppOrderBuilder
{
    /// <summary>
    /// ID 生成器接口，用于在构建订单时生成唯一标识符。
    /// </summary>
    private readonly IIdGenerator _idGenerator;

    // 内部状态字段，初始化为测试数据的默认值
    /// <summary>
    /// 订单 ID，初始化为测试数据中的有效订单 ID。
    /// </summary>
    private long _orderId = AppOrderTestData.ValidOrderId;

    /// <summary>
    /// 订单编号，初始化为测试数据中的有效订单编号。
    /// </summary>
    private string _orderNo = AppOrderTestData.ValidOrderNo;

    /// <summary>
    /// 产品 ID，初始化为测试数据中的有效产品 ID。
    /// </summary>
    private long _productId = AppOrderTestData.ValidProductId;

    /// <summary>
    /// 产品名称，初始化为测试数据中的有效产品名称。
    /// </summary>
    private string _productName = AppOrderTestData.ValidProductName;

    /// <summary>
    /// 产品代码，初始化为测试数据中的有效产品代码。
    /// </summary>
    private string _productCode = AppOrderTestData.ValidProductCode;

    /// <summary>
    /// 货币类型，初始化为测试数据中的有效货币。
    /// </summary>
    private string _currency = AppOrderTestData.ValidCurrency;

    /// <summary>
    /// 单价，初始化为测试数据中的有效单价。
    /// </summary>
    private decimal _unitPrice = AppOrderTestData.ValidUnitPrice;

    /// <summary>
    /// 数量，初始化为测试数据中的有效数量。
    /// </summary>
    private decimal _quantity = AppOrderTestData.ValidQuantity;

    /// <summary>
    /// 用户 ID，初始化为测试数据中的有效用户 ID。
    /// </summary>
    private long _userId = AppUserTestData.ValidUserId;

    /// <summary>
    /// 订单状态，初始化为测试数据中的有效订单状态。
    /// </summary>
    private AppOrderStatus _orderStatus = AppOrderTestData.ValidOrderStatus;

    /// <summary>
    /// 取消标志，初始化为测试数据中的有效取消状态。
    /// </summary>
    private bool _cancelled = AppOrderTestData.ValidCancelled;

    /// <summary>
    /// 地址 ID，初始化为测试数据中的有效地址 ID。
    /// </summary>
    private long _addressId = AppOrderTestData.ValidAddressId;

    /// <summary>
    /// 初始化 AppOrderBuilder 的新实例。
    /// </summary>
    /// <param name="idGenerator">ID 生成器实现，不能为 null。</param>
    /// <exception cref="ArgumentNullException">当 idGenerator 为 null 时抛出。</exception>
    public AppOrderBuilder(IIdGenerator idGenerator)
    {
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    /// <summary>
    /// 设置订单 ID。
    /// </summary>
    /// <param name="orderId">要设置的订单 ID。</param>
    /// <returns>当前的 AppOrderBuilder 实例，支持链式调用。</returns>
    public AppOrderBuilder WithOrderId(long orderId)
    {
        _orderId = orderId;
        return this;
    }

    /// <summary>
    /// 设置订单编号。
    /// </summary>
    /// <param name="orderNo">要设置的订单编号。</param>
    /// <returns>当前的 AppOrderBuilder 实例，支持链式调用。</returns>
    public AppOrderBuilder WithOrderNo(string orderNo)
    {
        _orderNo = orderNo;
        return this;
    }

    /// <summary>
    /// 设置产品 ID。
    /// </summary>
    /// <param name="productId">要设置的产品 ID。</param>
    /// <returns>当前的 AppOrderBuilder 实例，支持链式调用。</returns>
    public AppOrderBuilder WithProductId(long productId)
    {
        _productId = productId;
        return this;
    }

    /// <summary>
    /// 设置产品名称。
    /// </summary>
    /// <param name="productName">要设置的产品名称。</param>
    /// <returns>当前的 AppOrderBuilder 实例，支持链式调用。</returns>
    public AppOrderBuilder WithProductName(string productName)
    {
        _productName = productName;
        return this;
    }

    /// <summary>
    /// 设置产品代码。
    /// </summary>
    /// <param name="productCode">要设置的产品代码。</param>
    /// <returns>当前的 AppOrderBuilder 实例，支持链式调用。</returns>
    public AppOrderBuilder WithProductCode(string productCode)
    {
        _productCode = productCode;
        return this;
    }

    /// <summary>
    /// 设置货币类型。
    /// </summary>
    /// <param name="currency">要设置的货币类型。</param>
    /// <returns>当前的 AppOrderBuilder 实例，支持链式调用。</returns>
    public AppOrderBuilder WithCurrency(string currency)
    {
        _currency = currency;
        return this;
    }

    /// <summary>
    /// 设置单价。
    /// </summary>
    /// <param name="unitPrice">要设置的单价。</param>
    /// <returns>当前的 AppOrderBuilder 实例，支持链式调用。</returns>
    public AppOrderBuilder WithUnitPrice(decimal unitPrice)
    {
        _unitPrice = unitPrice;
        return this;
    }

    /// <summary>
    /// 设置数量。
    /// </summary>
    /// <param name="quantity">要设置的数量。</param>
    /// <returns>当前的 AppOrderBuilder 实例，支持链式调用。</returns>
    public AppOrderBuilder WithQuantity(decimal quantity)
    {
        _quantity = quantity;
        return this;
    }

    /// <summary>
    /// 设置用户 ID。
    /// </summary>
    /// <param name="userId">要设置的用户 ID。</param>
    /// <returns>当前的 AppOrderBuilder 实例，支持链式调用。</returns>
    public AppOrderBuilder WithUserId(long userId)
    {
        _userId = userId;
        return this;
    }

    /// <summary>
    /// 设置订单状态。
    /// </summary>
    /// <param name="status">要设置的订单状态。</param>
    /// <returns>当前的 AppOrderBuilder 实例，支持链式调用。</returns>
    public AppOrderBuilder WithOrderStatus(AppOrderStatus status)
    {
        _orderStatus = status;
        return this;
    }

    /// <summary>
    /// 设置取消标志。
    /// </summary>
    /// <param name="cancelled">要设置的取消状态。</param>
    /// <returns>当前的 AppOrderBuilder 实例，支持链式调用。</returns>
    public AppOrderBuilder WithCancelled(bool cancelled)
    {
        _cancelled = cancelled;
        return this;
    }

    /// <summary>
    /// 设置地址 ID。
    /// </summary>
    /// <param name="addressId">要设置的地址 ID。</param>
    /// <returns>当前的 AppOrderBuilder 实例，支持链式调用。</returns>
    public AppOrderBuilder WithAddressId(long addressId)
    {
        _addressId = addressId;
        return this;
    }

    /// <summary>
    /// 快捷方法：标记订单为已取消
    /// </summary>
    /// <returns>当前的 AppOrderBuilder 实例，支持链式调用。</returns>
    public AppOrderBuilder MarkAsCancelled()
    {
        _cancelled = true;
        return this;
    }

    /// <summary>
    /// 根据当前配置构建并返回一个 AppOrder 实例。
    /// 如果订单 ID 未被显式修改（仍为默认测试值），则使用默认 ID；否则生成新的唯一 ID。
    /// </summary>
    /// <returns>新创建的 AppOrder 实例。</returns>
    public AppOrder Build()
    {
        // 逻辑：如果 ID 不是默认值，则生成新 ID；否则使用设置的 ID
        // 注意：这与 AppUserBuilder 的逻辑保持一致
        var finalId = _orderId.Equals(AppOrderTestData.ValidOrderId)
            ? _orderId
            : _idGenerator.NewId();

        return new AppOrder
        {
            Id = finalId,
            OrderNo = _orderNo,
            ProductId = _productId,
            ProductName = _productName,
            ProductCode = _productCode,
            Currency = _currency,
            UnitPrice = _unitPrice,
            Quantity = _quantity,
            UserId = _userId,
            OccurredTime = DateTimeOffset.UtcNow,
            OrderStatus = _orderStatus,
            Cancelled = _cancelled,
            AddressId = _addressId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}


