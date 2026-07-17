using InprovePlan.UnitTests.TestData;
using InprovePlan.UserCase.Products.Commands;

namespace InprovePlan.UnitTests.Builders;

/// <summary>
/// 创建产品命令 (CreateProductCommand) 的测试数据构建器。
/// 用于在单元测试中快速构造合法或特定场景下的命令对象，
/// 避免在每个测试方法中重复编写繁琐的对象初始化代码。
/// </summary>
public sealed class CreateProductCommandBuilder
{
    // 默认产品编码：初始化为测试数据中定义的有效产品编码。
    // 这确保了在未显式指定时，构建出的命令包含一个符合业务规则的唯一标识符。
    private string _productCode = ProductTestData.ValidProductCode;

    // 默认产品名称：初始化为测试数据中定义的有效产品名称。
    private string _productName = ProductTestData.ValidProductName;

    // 默认产品描述：初始化为测试数据中定义的有效描述文本。
    private string _productDescription = ProductTestData.ValidProductDescription;

    // 默认产品类型ID：初始化为测试数据中定义的有效类型ID。
    // 这确保了产品关联到一个存在的分类或类型。
    private long _productTypeId = ProductTestData.ValidProductTypeId;

    // 默认单价：初始化为测试数据中定义的有效价格（通常为正数）。
    private decimal _unitPrice = ProductTestData.ValidUnitPrice;

    // 默认货币单位：初始化为测试数据中定义的有效货币代码（如 "CNY", "USD"）。
    private string _currency = ProductTestData.ValidCurrency;

    /// <summary>
    /// 设置产品编码。
    /// </summary>
    /// <param name="productCode">要设置的产品编码字符串。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法常用于测试产品编码唯一性校验、格式限制或非法字符拦截等场景。
    /// </remarks>
    public CreateProductCommandBuilder WithProductCode(string productCode)
    {
        // 更新内部产品编码字段
        _productCode = productCode;

        // 返回 this 以支持链式调用
        return this;
    }

    /// <summary>
    /// 设置货币单位。
    /// </summary>
    /// <param name="currency">要设置的货币代码字符串（例如 "CNY", "USD"）。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法常用于测试不支持的货币类型、空货币代码或格式错误的货币标识。
    /// </remarks>
    public CreateProductCommandBuilder WithCurrency(string currency)
    {
        // 更新内部货币单位字段
        _currency = currency;

        // 返回 this 以支持链式调用
        return this;
    }

    /// <summary>
    /// 构建并返回最终的 CreateProductCommand 对象。
    /// </summary>
    /// <returns>包含当前配置参数的命令对象实例。</returns>
    /// <remarks>
    /// 此方法是构建过程的终点。它使用当前内部存储的各字段值
    /// 实例化命令对象。在此之后，构建器通常不再被使用，或者可以重置以构建新对象。
    /// </remarks>
    public CreateProductCommand Build()
    {
        // 使用当前配置好的参数创建命令对象
        return new CreateProductCommand(
            _productCode,
            _productName,
            _productDescription,
            _productTypeId,
            _unitPrice,
            _currency);
    }
}

