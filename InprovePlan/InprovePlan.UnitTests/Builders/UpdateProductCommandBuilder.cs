using InprovePlan.Domain.Entities;
using InprovePlan.UnitTests.TestData;
using InprovePlan.UserCase.Products.Commands;

namespace InprovePlan.UnitTests.Builders;

/// <summary>
/// 更新产品信息命令 (UpdateProductCommand) 的测试数据构建器。
/// 用于在单元测试中快速构造合法或特定场景下的更新命令对象，
/// 避免在每个测试方法中重复编写繁琐的对象初始化代码。
/// </summary>
public sealed class UpdateProductCommandBuilder
{
    // 默认产品ID：初始化为测试数据中定义的有效产品ID。
    // 这确保了在未显式指定ID时，构建出的命令指向一个存在的产品记录。
    private long _id = ProductTestData.ValidProductId;

    // 默认产品名称：初始化为测试数据中定义的有效产品名称。
    private string _productName = ProductTestData.ValidProductName;

    // 默认产品描述：初始化为测试数据中定义的有效描述文本。
    private string _productDescription = ProductTestData.ValidProductDescription;

    // 默认产品类型ID：初始化为测试数据中定义的有效类型ID。
    // 这确保产品关联到一个存在的分类或类型。
    private long _productTypeId = ProductTestData.ValidProductTypeId;

    // 默认产品状态：初始化为测试数据中定义的有效状态（如上架、下架等）。
    private AppProductStatus _productStatus = ProductTestData.ValidProductStatus;

    // 默认单价：初始化为测试数据中定义的有效价格（通常为正数）。
    private decimal _unitPrice = ProductTestData.ValidUnitPrice;

    // 默认货币单位：初始化为测试数据中定义的有效货币代码（如 "CNY", "USD"）。
    private string _currency = ProductTestData.ValidCurrency;

    /// <summary>
    /// 设置产品状态。
    /// </summary>
    /// <param name="productStatus">要设置的目标产品状态。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法允许测试人员自定义产品状态，例如测试从上架变更为下架，
    /// 或测试非法的状态转换逻辑。
    /// </remarks>
    public UpdateProductCommandBuilder WithProductStatus(AppProductStatus productStatus)
    {
        // 更新内部产品状态字段
        _productStatus = productStatus;

        // 返回 this 以支持链式调用
        return this;
    }

    /// <summary>
    /// 构建并返回最终的 UpdateProductCommand 对象。
    /// </summary>
    /// <returns>包含当前配置参数的命令对象实例。</returns>
    /// <remarks>
    /// 此方法是构建过程的终点。它使用当前内部存储的各字段值
    /// 实例化命令对象。在此之后，构建器通常不再被使用，或者可以重置以构建新对象。
    /// </remarks>
    public UpdateProductCommand Build()
    {
        // 使用当前配置好的参数创建更新命令对象
        return new UpdateProductCommand(
            _id,
            _productName,
            _productDescription,
            _productTypeId,
            _productStatus,
            _unitPrice,
            _currency);
    }
}

