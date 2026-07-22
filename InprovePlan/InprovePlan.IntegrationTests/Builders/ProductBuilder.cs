using InprovePlan.Domain.Entities;
using InprovePlan.IntegrationTests.TestData;
using Instructure.Interfaces;

namespace InprovePlan.IntegrationTests.Builders;

/// <summary>
/// 产品构建器，用于创建和配置 Product 实例。
/// 采用建造者模式，支持链式调用以灵活设置产品属性，并在构建时应用特定的业务规则（如货币格式化、价格精度处理）。
/// </summary>
public class ProductBuilder
{
    /// <summary>
    /// ID 生成器接口，用于在构建产品时生成唯一标识符。
    /// </summary>
    private readonly IIdGenerator _idGenerator;

    /// <summary>
    /// 产品 ID，初始化为测试数据中的有效产品 ID。
    /// </summary>
    private long _productId = ProductTestData.ValidProductId;

    /// <summary>
    /// 产品代码，初始化为测试数据中的有效产品代码。
    /// </summary>
    private string _productCode = ProductTestData.ValidProductCode;

    /// <summary>
    /// 产品名称，初始化为测试数据中的有效产品名称。
    /// </summary>
    private string _productName = ProductTestData.ValidProductName;

    /// <summary>
    /// 产品描述，初始化为测试数据中的有效产品描述。
    /// </summary>
    private string _productDescription = ProductTestData.ValidProductDescription;

    /// <summary>
    /// 产品类型 ID，初始化为测试数据中的有效产品类型 ID。
    /// </summary>
    private long _productTypeId = ProductTestData.ValidProductTypeId;

    /// <summary>
    /// 产品状态，初始化为测试数据中的有效产品状态。
    /// </summary>
    private AppProductStatus _productStatus = ProductTestData.ValidProductStatus;

    /// <summary>
    /// 单价，初始化为测试数据中的有效单价。
    /// </summary>
    private decimal _unitPrice = ProductTestData.ValidUnitPrice;

    /// <summary>
    /// 货币类型，初始化为测试数据中的有效货币。
    /// </summary>
    private string _currency = ProductTestData.ValidCurrency;

    /// <summary>
    /// 初始化 ProductBuilder 的新实例。
    /// </summary>
    /// <param name="idGenerator">ID 生成器实现，不能为 null。</param>
    /// <exception cref="ArgumentNullException">当 idGenerator 为 null 时抛出。</exception>
    public ProductBuilder(IIdGenerator idGenerator)
    {
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    /// <summary>
    /// 设置产品 ID。
    /// </summary>
    /// <param name="productId">要设置的产品 ID。</param>
    /// <returns>当前的 ProductBuilder 实例，支持链式调用。</returns>
    public ProductBuilder WithProductId(long productId)
    {
        _productId = productId;
        return this;
    }

    /// <summary>
    /// 设置产品代码。
    /// </summary>
    /// <param name="productCode">要设置的产品代码。</param>
    /// <returns>当前的 ProductBuilder 实例，支持链式调用。</returns>
    public ProductBuilder WithProductCode(string productCode)
    {
        _productCode = productCode;
        return this;
    }

    /// <summary>
    /// 设置产品名称。
    /// </summary>
    /// <param name="productName">要设置的产品名称。</param>
    /// <returns>当前的 ProductBuilder 实例，支持链式调用。</returns>
    public ProductBuilder WithProductName(string productName)
    {
        _productName = productName;
        return this;
    }

    /// <summary>
    /// 设置产品描述。
    /// </summary>
    /// <param name="description">要设置的产品描述。</param>
    /// <returns>当前的 ProductBuilder 实例，支持链式调用。</returns>
    public ProductBuilder WithProductDescription(string description)
    {
        _productDescription = description;
        return this;
    }

    /// <summary>
    /// 设置产品类型 ID。
    /// </summary>
    /// <param name="productTypeId">要设置的产品类型 ID。</param>
    /// <returns>当前的 ProductBuilder 实例，支持链式调用。</returns>
    public ProductBuilder WithProductTypeId(long productTypeId)
    {
        _productTypeId = productTypeId;
        return this;
    }

    /// <summary>
    /// 设置产品状态。
    /// </summary>
    /// <param name="status">要设置的产品状态。</param>
    /// <returns>当前的 ProductBuilder 实例，支持链式调用。</returns>
    public ProductBuilder WithProductStatus(AppProductStatus status)
    {
        _productStatus = status;
        return this;
    }

    /// <summary>
    /// 设置单价。
    /// </summary>
    /// <param name="unitPrice">要设置的单价。</param>
    /// <returns>当前的 ProductBuilder 实例，支持链式调用。</returns>
    public ProductBuilder WithUnitPrice(decimal unitPrice)
    {
        _unitPrice = unitPrice;
        return this;
    }

    /// <summary>
    /// 设置货币类型。
    /// </summary>
    /// <param name="currency">要设置的货币类型。</param>
    /// <returns>当前的 ProductBuilder 实例，支持链式调用。</returns>
    public ProductBuilder WithCurrency(string currency)
    {
        _currency = currency;
        return this;
    }

    /// <summary>
    /// 根据当前配置构建并返回一个 Product 实例。
    /// 在构建过程中应用以下业务规则：
    /// 1. ID 生成：如果产品 ID 未被显式修改（仍为默认测试值），则使用默认 ID；否则生成新的唯一 ID。
    /// 2. 货币格式化：将货币代码转换为大写不变形式。
    /// 3. 价格精度：将单价保留两位小数。
    /// </summary>
    /// <returns>新创建的 Product 实例。</returns>
    public Product Build()
    {
        // 1. ID 生成逻辑：如果不是默认值，则生成新 ID；否则使用设置的值
        var finalId = _productId.Equals(ProductTestData.ValidProductId)
            ? _productId
            : _idGenerator.NewId();

        // 2. 业务规则处理：货币大写
        var finalCurrency = _currency?.ToUpperInvariant() ?? string.Empty;

        // 3. 业务规则处理：价格保留两位小数
        var finalUnitPrice = Math.Round(_unitPrice, 2);

        return new Product
        {
            Id = finalId,
            ProductCode = _productCode,
            ProductName = _productName,
            ProductDescription = _productDescription,
            ProductTypeId = _productTypeId,
            ProductStatus = _productStatus,
            UnitPrice = finalUnitPrice,
            Currency = finalCurrency,

            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}


