using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InprovePlan.ApiTests.Infrastructure;
using InprovePlan.Domain.Entities;
using Instructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InprovePlan.ApiTests.Products;

/// <summary>
/// 商品接口测试。
/// </summary>
public sealed class ProductControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// 测试场景：登录用户新增商品。
    /// 预期结果：返回成功，商品状态默认为 Enable。
    /// </summary>
    [Fact]
    public async Task Create_ShouldReturnCreatedProduct()
    {
        await ResetAsync();

        var client = _factory.CreateClient();
        var user = await CreateUserAsync(client, "product_create_user", "13900003001", "product_create_user@example.com");
        _factory.CurrentUser.Id = user.Id;

        var response = await client.PostAsJsonAsync(
            "/api/Product",
            new
            {
                ProductCode = "API-PRODUCT-001",
                ProductName = "api_product_001",
                ProductDescription = "符合 ProductConfiguration 长度要求的商品描述。",
                ProductTypeId = 1,
                UnitPrice = 99.99m,
                Currency = "CNY"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<ProductDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.ProductCode.Should().Be("API-PRODUCT-001");
        body.Data.ProductStatus.Should().Be((int)AppProductStatus.Enable);
        body.Data.UnitPrice.Should().Be(99.99m);
        body.Data.Currency.Should().Be("CNY");
    }

    /// <summary>
    /// 测试场景：修改商品基础信息。
    /// 预期结果：返回成功，商品名称、描述、分类、状态、价格、币种被更新。
    /// </summary>
    [Fact]
    public async Task Update_ShouldReturnUpdatedProduct()
    {
        await ResetAsync();

        var client = _factory.CreateClient();
        var user = await CreateUserAsync(client, "product_update_user", "13900003002", "product_update_user@example.com");
        _factory.CurrentUser.Id = user.Id;

        var product = await CreateProductAsync(client, "API-PRODUCT-UPDATE", "api_product_update", 1);

        var response = await client.PutAsJsonAsync(
            $"/api/Product/{product.Id}",
            new
            {
                ProductName = "api_product_updated",
                ProductDescription = "更新后的商品描述。",
                ProductTypeId = 2,
                ProductStatus = AppProductStatus.SoldOut,
                UnitPrice = 188.88m,
                Currency = "USD"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<ProductDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.ProductName.Should().Be("api_product_updated");
        body.Data.ProductTypeId.Should().Be(2);
        body.Data.ProductStatus.Should().Be((int)AppProductStatus.SoldOut);
        body.Data.UnitPrice.Should().Be(188.88m);
        body.Data.Currency.Should().Be("USD");
    }

    /// <summary>
    /// 测试场景：删除商品。
    /// 预期结果：商品不会物理删除，ProductStatus 变为 Void。
    /// </summary>
    [Fact]
    public async Task Delete_ShouldMarkProductAsVoid()
    {
        await ResetAsync();

        var client = _factory.CreateClient();
        var user = await CreateUserAsync(client, "product_delete_user", "13900003003", "product_delete_user@example.com");
        _factory.CurrentUser.Id = user.Id;

        var product = await CreateProductAsync(client, "API-PRODUCT-DELETE", "api_product_delete", 1);

        var response = await client.DeleteAsync(
            $"/api/Product/{product.Id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var entity = dbContext.Set<Product>().Single(x => x.Id == product.Id);
        entity.ProductStatus.Should().Be(AppProductStatus.Void);
    }

    /// <summary>
    /// 测试场景：按 Id 查询商品。
    /// 预期结果：返回指定商品。
    /// </summary>
    [Fact]
    public async Task GetById_ShouldReturnProduct()
    {
        await ResetAsync();

        var client = _factory.CreateClient();
        var user = await CreateUserAsync(client, "product_get_user", "13900003004", "product_get_user@example.com");
        _factory.CurrentUser.Id = user.Id;

        var product = await CreateProductAsync(client, "API-PRODUCT-GET", "api_product_get", 1);

        var response = await client.GetAsync(
            $"/api/Product/{product.Id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<ProductDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Id.Should().Be(product.Id);
        body.Data.ProductCode.Should().Be("API-PRODUCT-GET");
    }

    /// <summary>
    /// 测试场景：分页查询商品。
    /// 预期结果：只返回符合 keyword、productTypeId、productStatus 的商品。
    /// </summary>
    [Fact]
    public async Task GetPaged_ShouldReturnPagedProducts()
    {
        await ResetAsync();

        var client = _factory.CreateClient();
        var user = await CreateUserAsync(client, "product_page_user", "13900003005", "product_page_user@example.com");
        _factory.CurrentUser.Id = user.Id;

        await CreateProductAsync(client, "API-PAGE-PRODUCT-001", "page_product_001", 1);
        await CreateProductAsync(client, "API-PAGE-PRODUCT-002", "page_product_002", 1);
        await CreateProductAsync(client, "API-PAGE-PRODUCT-003", "other_product_003", 2);

        var response = await client.GetAsync(
            "/api/Product?pageIndex=1&pageSize=10&sortBy=createdAt&sortDirection=desc&keyword=page_product&productTypeId=1&productStatus=1",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<PagedResultJson<ProductDtoJson>>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Total.Should().Be(2);
        body.Data.Items.Should().HaveCount(2);
        body.Data.Items.Should().OnlyContain(x => x.ProductTypeId == 1);
        body.Data.Items.Should().OnlyContain(x => x.ProductStatus == (int)AppProductStatus.Enable);
    }

    private async Task ResetAsync()
    {
        _factory.CurrentUser.Id = null;
        await _factory.ResetDatabaseAsync();
    }

    private static async Task<AppUserDtoJson> CreateUserAsync(HttpClient client, string userName, string phoneNumber, string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/AppUser",
            new
            {
                UserName = userName,
                Password = "Password123?",
                Sex = AppUserSex.Secret,
                PhoneNumber = phoneNumber,
                Email = email
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppUserDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }

    private static async Task<ProductDtoJson> CreateProductAsync(HttpClient client, string productCode, string productName, int productTypeId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/Product",
            new
            {
                ProductCode = productCode,
                ProductName = productName,
                ProductDescription = "符合 ProductConfiguration 长度要求的商品描述。",
                ProductTypeId = productTypeId,
                UnitPrice = 99.99m,
                Currency = "CNY"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<ProductDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }

    private sealed class ApiResponseJson<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }

    private sealed class AppUserDtoJson
    {
        public long Id { get; set; }
    }

    private sealed class ProductDtoJson
    {
        public long Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int ProductTypeId { get; set; }
        public int ProductStatus { get; set; }
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; } = string.Empty;
    }

    private sealed class PagedResultJson<T>
    {
        public long Total { get; set; }
        public int Count { get; set; }
        public List<T> Items { get; set; } = [];
    }
}