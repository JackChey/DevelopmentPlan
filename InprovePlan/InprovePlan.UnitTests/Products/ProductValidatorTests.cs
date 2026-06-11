using FluentAssertions;
using InprovePlan.Domain.Entities;
using InprovePlan.UnitTests.TestDoubles;
using InprovePlan.UserCase.Products.Commands;
using InprovePlan.UserCase.Products.Queries;
using Instructure.Configurations.Entities;
using Xunit;

namespace InprovePlan.UnitTests.Products;

/// <summary>
/// 商品参数校验测试。
/// </summary>
public sealed class ProductValidatorTests
{
    /// <summary>
    /// 测试用例：新增商品参数全部合法。
    /// 预期结果：校验通过。
    /// </summary>
    [Fact]
    public void Create_ShouldPass_WhenCommandIsValid()
    {
        var validator = new CreateProductCommandValidator();

        var result = validator.Validate(UnitTestDataFactory.CreateProductCommand());

        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// 测试用例：ProductCode 超过数据库配置长度。
    /// 预期结果：校验失败。
    /// </summary>
    [Fact]
    public void Create_ShouldFail_WhenProductCodeIsTooLong()
    {
        var validator = new CreateProductCommandValidator();

        var command = UnitTestDataFactory.CreateProductCommand(
            productCode: new string('A', DataSchemaConstants.ProductCodeLength + 1));

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    /// <summary>
    /// 测试用例：Currency 不是 3 位。
    /// 预期结果：校验失败。
    /// </summary>
    [Fact]
    public void Create_ShouldFail_WhenCurrencyLengthIsInvalid()
    {
        var validator = new CreateProductCommandValidator();

        var command = UnitTestDataFactory.CreateProductCommand(currency: "CN");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    /// <summary>
    /// 测试用例：修改商品时 ProductStatus 不是合法枚举。
    /// 预期结果：校验失败。
    /// </summary>
    [Fact]
    public void Update_ShouldFail_WhenProductStatusIsInvalid()
    {
        var validator = new UpdateProductCommandValidator();

        var command = UnitTestDataFactory.UpdateProductCommand(
            productStatus: (AppProductStatus)999);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    /// <summary>
    /// 测试用例：删除商品时 Id 为 0。
    /// 预期结果：校验失败。
    /// </summary>
    [Fact]
    public void Delete_ShouldFail_WhenIdIsInvalid()
    {
        var validator = new DeleteProductCommandValidator();

        var result = validator.Validate(UnitTestDataFactory.DeleteProductCommand(id: 0));

        result.IsValid.Should().BeFalse();
    }

    /// <summary>
    /// 测试用例：分页查询商品参数合法。
    /// 预期结果：校验通过。
    /// </summary>
    [Fact]
    public void GetPaged_ShouldPass_WhenQueryIsValid()
    {
        var validator = new GetProductsPagedQueryValidator();

        var result = validator.Validate(UnitTestDataFactory.GetProductsPagedQuery());

        result.IsValid.Should().BeTrue();
    }
}