using FluentAssertions;
using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.Products.Queries;
using Instructure.Paging;
using Instructure.Repositories;
using Instructure.Sorting;
using Xunit;

namespace InprovePlan.IntegrationTests.UseCases.Products;

/// <summary>
/// 商品查询处理器集成测试。
/// </summary>
[Collection(Infrastructure.IntegrationTestCollection.Name)]
public sealed class ProductQueryHandlerTests
{
    private readonly Infrastructure.MySqlTestFixture _fixture;

    public ProductQueryHandlerTests(Infrastructure.MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// 测试场景：按 Id 查询有效商品。
    /// 预期结果：返回商品 DTO。
    /// </summary>
    [Fact]
    public async Task GetById_ShouldReturnProduct()
    {
        await _fixture.ResetDatabaseAsync();

        var product = await SeedProductAsync("IT-PRODUCT-GET", "查询商品", 1, AppProductStatus.Enable);
        var handler = new GetProductByIdQueryHandler(new EfReadRepository<Product>(_fixture.DbContext));

        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(product.Id);
        result.Value.ProductCode.Should().Be("IT-PRODUCT-GET");
    }

    /// <summary>
    /// 测试场景：按 Id 查询 Void 商品。
    /// 预期结果：返回失败，提示商品不存在。
    /// </summary>
    [Fact]
    public async Task GetById_ShouldFail_WhenProductIsVoid()
    {
        await _fixture.ResetDatabaseAsync();

        var product = await SeedProductAsync("IT-PRODUCT-VOID", "作废商品", 1, AppProductStatus.Void);
        var handler = new GetProductByIdQueryHandler(new EfReadRepository<Product>(_fixture.DbContext));

        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    /// <summary>
    /// 测试场景：分页查询商品。
    /// 预期结果：按 keyword、productTypeId、productStatus、includeVoid 过滤。
    /// </summary>
    [Fact]
    public async Task GetPaged_ShouldReturnMatchedProducts()
    {
        await _fixture.ResetDatabaseAsync();

        await SeedProductAsync("IT-PAGE-PRODUCT-001", "page_product_001", 1, AppProductStatus.Enable);
        await SeedProductAsync("IT-PAGE-PRODUCT-002", "page_product_002", 1, AppProductStatus.Enable);
        await SeedProductAsync("IT-PAGE-PRODUCT-003", "page_product_003", 2, AppProductStatus.Enable);
        await SeedProductAsync("IT-PAGE-PRODUCT-004", "page_product_004", 1, AppProductStatus.Void);

        var handler = new GetProductsPagedQueryHandler(new EfReadRepository<Product>(_fixture.DbContext));

        var result = await handler.Handle(
            new GetProductsPagedQuery(
                new Pagination { PageIndex = 1, PageSize = 10 },
                new SortQuery { SortBy = "createdAt", SortDirection = "desc" },
                "page_product",
                1,
                AppProductStatus.Enable,
                false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(2);
        result.Value.Items.Should().OnlyContain(x => x.ProductTypeId == 1);
        result.Value.Items.Should().OnlyContain(x => x.ProductStatus == AppProductStatus.Enable);
    }

    private async Task<Product> SeedProductAsync(string code, string name, int typeId, AppProductStatus status)
    {
        var product = new Product
        {
            Id = _fixture.IdGenerator.NewId(),
            ProductCode = code,
            ProductName = name,
            ProductDescription = "符合 ProductConfiguration 长度要求的商品描述。",
            ProductTypeId = typeId,
            ProductStatus = status,
            UnitPrice = 99.99m,
            Currency = "CNY",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _fixture.DbContext.Set<Product>().AddAsync(product, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return product;
    }
}