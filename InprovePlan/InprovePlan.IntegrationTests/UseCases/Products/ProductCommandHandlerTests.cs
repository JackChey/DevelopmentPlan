using FluentAssertions;
using InprovePlan.Domain.Entities;
using InprovePlan.IntegrationTests.Infrastructure;
using InprovePlan.UserCase.Products.Commands;
using Instructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InprovePlan.IntegrationTests.UseCases.Products;

/// <summary>
/// 商品命令处理器集成测试。
/// </summary>
[Collection(Infrastructure.IntegrationTestCollection.Name)]
public sealed class ProductCommandHandlerTests
{
    private readonly Infrastructure.MySqlTestFixture _fixture;

    public ProductCommandHandlerTests(Infrastructure.MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// 测试场景：新增商品。
    /// 预期结果：商品编码转大写，价格保留 2 位小数，状态为 Enable。
    /// </summary>
    [Fact]
    public async Task Create_ShouldCreateProduct()
    {
        await _fixture.ResetDatabaseAsync();

        var handler = new CreateProductCommandHandler(new EfRepository<Product>(_fixture.DbContext));

        var result = await handler.Handle(
            new CreateProductCommand("it-product-001", "集成测试商品001", "商品描述。", 1, 88.889m, "cny"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductCode.Should().Be("IT-PRODUCT-001");
        result.Value.UnitPrice.Should().Be(88.89m);
        result.Value.Currency.Should().Be("CNY");

        var product = await _fixture.DbContext.Set<Product>()
            .SingleAsync(x => x.ProductCode == "IT-PRODUCT-001", TestContext.Current.CancellationToken);

        product.ProductStatus.Should().Be(AppProductStatus.Enable);
    }

    /// <summary>
    /// 测试场景：新增重复商品编码。
    /// 预期结果：返回失败，不写入重复商品。
    /// </summary>
    [Fact]
    public async Task Create_ShouldFail_WhenProductCodeExists()
    {
        await _fixture.ResetDatabaseAsync();

        var handler = new CreateProductCommandHandler(new EfRepository<Product>(_fixture.DbContext));
        var command = new CreateProductCommand("IT-PRODUCT-DUP", "重复商品", "商品描述。", 1, 10.00m, "CNY");

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeFalse();

        var count = await _fixture.DbContext.Set<Product>()
            .CountAsync(x => x.ProductCode == "IT-PRODUCT-DUP", TestContext.Current.CancellationToken);

        count.Should().Be(1);
    }

    /// <summary>
    /// 测试场景：修改有效商品。
    /// 预期结果：商品基础信息被更新。
    /// </summary>
    [Fact]
    public async Task Update_ShouldUpdateProduct()
    {
        await _fixture.ResetDatabaseAsync();

        var product = await SeedProductAsync("IT-PRODUCT-UPDATE", "旧商品", 1, AppProductStatus.Enable);
        var handler = new UpdateProductCommandHandler(new EfRepository<Product>(_fixture.DbContext));

        var result = await handler.Handle(
            new UpdateProductCommand(product.Id, "新商品", "新描述。", 2, AppProductStatus.SoldOut, 199.999m, "usd"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductName.Should().Be("新商品");
        result.Value.ProductTypeId.Should().Be(2);
        result.Value.ProductStatus.Should().Be(AppProductStatus.SoldOut);
        result.Value.UnitPrice.Should().Be(200.00m);
        result.Value.Currency.Should().Be("USD");
    }

    /// <summary>
    /// 测试场景：删除商品。
    /// 预期结果：ProductStatus 更新为 Void。
    /// </summary>
    [Fact]
    public async Task Delete_ShouldMarkProductAsVoid()
    {
        await _fixture.ResetDatabaseAsync();

        var product = await SeedProductAsync("IT-PRODUCT-DELETE", "待作废商品", 1, AppProductStatus.Enable);
        var handler = new DeleteProductCommandHandler(new EfRepository<Product>(_fixture.DbContext));

        var result = await handler.Handle(new DeleteProductCommand(product.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var entity = await _fixture.DbContext.Set<Product>()
            .SingleAsync(x => x.Id == product.Id, TestContext.Current.CancellationToken);

        entity.ProductStatus.Should().Be(AppProductStatus.Void);
    }

    private async Task<Product> SeedProductAsync(string code, string name, int typeId, AppProductStatus status)
    {

        var product = TestEntityFactory.CreateProduct(
            _fixture.IdGenerator,
            productCode: "IT-PRODUCT-001",
            productName: "集成测试商品001");

        await _fixture.DbContext.Set<Product>().AddAsync(
            product,
            TestContext.Current.CancellationToken);

        await _fixture.DbContext.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        return product;
    }
}