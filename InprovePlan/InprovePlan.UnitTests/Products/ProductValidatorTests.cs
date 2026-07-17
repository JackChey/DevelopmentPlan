using InprovePlan.UnitTests.Builders;
using InprovePlan.UnitTests.TestData;
using InprovePlan.UserCase.Products.Commands;
using InprovePlan.UserCase.Products.Queries;

namespace InprovePlan.UnitTests.Products;

using FluentValidation.TestHelper; // 假设使用了 FluentValidation 的测试辅助功能，虽然这里主要用自定义扩展
using Xunit;
using FluentAssertions;

/// <summary>
/// 商品参数校验器 (Product Validators) 的单元测试类。
/// 
/// 主要验证以下场景：
/// 1. 创建商品 (CreateProductCommand) 的参数合法性。
/// 2. 更新商品 (UpdateProductCommand) 的状态合法性。
/// 3. 删除商品 (DeleteProductCommand) 的 ID 合法性。
/// 4. 分页查询商品 (GetProductsPagedQuery) 的参数合法性。
/// 
/// 使用 Builder 模式构造测试数据，确保测试用例的可读性和维护性。
/// </summary>
public sealed class ProductValidatorTests
{
    /// <summary>
    /// 测试创建商品命令在参数完全合法时，验证应当通过。
    /// </summary>
    /// <remarks>
    /// 场景描述：
    /// - 使用 CreateProductCommandBuilder 构建一个包含默认有效数据的命令对象。
    /// - 使用 CreateProductCommandValidator 进行验证。
    /// 
    /// 预期结果：
    /// - 验证结果 IsValid 为 true，且没有错误信息。
    /// </remarks>
    [Fact]
    public void Create_WhenCommandIsValid_ShouldPass()
    {
        // Arrange: 初始化验证器和合法的命令对象
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommandBuilder().Build();

        // Act: 执行验证
        var result = validator.Validate(command);

        // Assert: 验证应当通过
        result.ShouldPassValidation();
    }

    /// <summary>
    /// 测试当商品编码 (ProductCode) 过长时，验证应当失败并报告特定错误。
    /// </summary>
    /// <remarks>
    /// 场景描述：
    /// - 使用 Builder 设置一个超过最大长度限制的 ProductCode (TooLongProductCode)。
    /// 
    /// 预期结果：
    /// - 验证失败。
    /// - 错误列表中恰好包含一个错误，且该错误针对的是 "ProductCode" 字段。
    /// </remarks>
    [Fact]
    public void Create_WhenProductCodeIsTooLong_ShouldHaveProductCodeValidationError()
    {
        // Arrange: 构建包含非法长编码的命令对象
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommandBuilder()
            .WithProductCode(ProductTestData.TooLongProductCode) // 设置过长的编码
            .Build();

        // Act: 执行验证
        var result = validator.Validate(command);

        // Assert: 应当产生针对 ProductCode 的单一验证错误
        result.ShouldHaveSingleValidationErrorFor(nameof(CreateProductCommand.ProductCode));
    }

    /// <summary>
    /// 测试当货币单位 (Currency) 格式或长度无效时，验证应当失败并报告特定错误。
    /// </summary>
    /// <remarks>
    /// 场景描述：
    /// - 使用 Builder 设置一个无效的货币代码 (InvalidCurrency)，例如长度不为3或包含非法字符。
    /// 
    /// 预期结果：
    /// - 验证失败。
    /// - 错误列表中恰好包含一个错误，且该错误针对的是 "Currency" 字段。
    /// </remarks>
    [Fact]
    public void Create_WhenCurrencyLengthIsInvalid_ShouldHaveCurrencyValidationError()
    {
        // Arrange: 构建包含非法货币单位的命令对象
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommandBuilder()
            .WithCurrency(ProductTestData.InvalidCurrency) // 设置无效的货币代码
            .Build();

        // Act: 执行验证
        var result = validator.Validate(command);

        // Assert: 应当产生针对 Currency 的单一验证错误
        result.ShouldHaveSingleValidationErrorFor(nameof(CreateProductCommand.Currency));
    }

    /// <summary>
    /// 测试当更新商品命令中的产品状态 (ProductStatus) 无效时，验证应当失败并报告特定错误。
    /// </summary>
    /// <remarks>
    /// 场景描述：
    /// - 使用 UpdateProductCommandBuilder 设置一个不在枚举定义范围内或业务上不允许的状态 (InvalidProductStatus)。
    /// - 使用 UpdateProductCommandValidator 进行验证。
    /// 
    /// 预期结果：
    /// - 验证失败。
    /// - 错误列表中恰好包含一个错误，且该错误针对的是 "ProductStatus" 字段。
    /// </remarks>
    [Fact]
    public void Update_WhenProductStatusIsInvalid_ShouldHaveProductStatusValidationError()
    {
        // Arrange: 构建包含非法状态的更新命令对象
        var validator = new UpdateProductCommandValidator();
        var command = new UpdateProductCommandBuilder()
            .WithProductStatus(ProductTestData.InvalidProductStatus) // 设置无效的状态
            .Build();

        // Act: 执行验证
        var result = validator.Validate(command);

        // Assert: 应当产生针对 ProductStatus 的单一验证错误
        result.ShouldHaveSingleValidationErrorFor(nameof(UpdateProductCommand.ProductStatus));
    }

    /// <summary>
    /// 测试当删除商品命令中的 ID 无效时，验证应当失败并报告特定错误。
    /// </summary>
    /// <remarks>
    /// 场景描述：
    /// - 直接使用 DeleteProductCommand 构造函数传入一个无效的 ID (InvalidProductId)，例如 0 或负数。
    /// - 使用 DeleteProductCommandValidator 进行验证。
    /// 
    /// 预期结果：
    /// - 验证失败。
    /// - 错误列表中恰好包含一个错误，且该错误针对的是 "Id" 字段。
    /// </remarks>
    [Fact]
    public void Delete_WhenIdIsInvalid_ShouldHaveIdValidationError()
    {
        // Arrange: 构建包含非法 ID 的删除命令对象
        var validator = new DeleteProductCommandValidator();
        var command = new DeleteProductCommand(ProductTestData.InvalidProductId); // 直接传入无效 ID

        // Act: 执行验证
        var result = validator.Validate(command);

        // Assert: 应当产生针对 Id 的单一验证错误
        result.ShouldHaveSingleValidationErrorFor(nameof(DeleteProductCommand.Id));
    }

    /// <summary>
    /// 测试分页查询商品命令在参数完全合法时，验证应当通过。
    /// </summary>
    /// <remarks>
    /// 场景描述：
    /// - 使用 GetProductsPagedQueryBuilder 构建一个包含默认有效分页、排序和筛选条件的查询对象。
    /// - 使用 GetProductsPagedQueryValidator 进行验证。
    /// 
    /// 预期结果：
    /// - 验证结果 IsValid 为 true，且没有错误信息。
    /// </remarks>
    [Fact]
    public void GetPaged_WhenQueryIsValid_ShouldPass()
    {
        // Arrange: 初始化验证器和合法的查询对象
        var validator = new GetProductsPagedQueryValidator();
        var query = new GetProductsPagedQueryBuilder().Build();

        // Act: 执行验证
        var result = validator.Validate(query);

        // Assert: 验证应当通过
        result.ShouldPassValidation();
    }
}

