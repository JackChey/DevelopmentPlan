using InprovePlan.ApiTests.Clients;
using InprovePlan.ApiTests.Infrastructure;
using InprovePlan.ApiTests.TestData;
using InprovePlan.Domain.Entities;

namespace InprovePlan.ApiTests.Products;

[Collection(WebApiTestCollection.Name)]
public sealed class ProductControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WhenRequestValid_ShouldCreateProduct()
    {
        var client = await CreateClientWithCurrentUserAsync();

        var product = await CreateProductAsync(client);

        product.Id.Should().BeGreaterThan(0);
        product.ProductCode.Should().Be(ProductTestData.ValidProductCode.ToUpperInvariant());
        product.ProductStatus.Should().Be((int)AppProductStatus.Enable);
    }

    [Fact]
    public async Task Update_WhenProductExists_ShouldUpdateProduct()
    {
        var client = await CreateClientWithCurrentUserAsync();
        var product = await CreateProductAsync(client);

        var updated = await ProductApiClient.UpdateProductAsync(
            client,
            product.Id,
            "Updated Product",
            "Updated product description",
            2,
            AppProductStatus.SoldOut,
            88.886m,
            "usd");

        updated.Id.Should().Be(product.Id);
        updated.ProductName.Should().Be("Updated Product");
        updated.ProductStatus.Should().Be((int)AppProductStatus.SoldOut);
        updated.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Delete_WhenProductExists_ShouldMarkProductAsVoid()
    {
        var client = await CreateClientWithCurrentUserAsync();
        var product = await CreateProductAsync(client);

        await ProductApiClient.DeleteProductAsync(client, product.Id);
    }

    [Fact]
    public async Task GetById_WhenProductExists_ShouldReturnProduct()
    {
        var client = await CreateClientWithCurrentUserAsync();
        var product = await CreateProductAsync(client);

        var found = await ProductApiClient.GetProductByIdAsync(client, product.Id);

        found.Id.Should().Be(product.Id);
        found.ProductCode.Should().Be(ProductTestData.ValidProductCode.ToUpperInvariant());
    }

    [Fact]
    public async Task GetPaged_WhenProductsExist_ShouldReturnPagedProducts()
    {
        var client = await CreateClientWithCurrentUserAsync();
        var product = await CreateProductAsync(client);

        var page = await ProductApiClient.GetProductsPagedAsync(
            client,
            keyword: ProductTestData.ValidProductName,
            productTypeId: 1,
            productStatus: AppProductStatus.Enable);

        page.Total.Should().Be(1);
        page.Items.Should().ContainSingle(item => item.Id == product.Id);
    }

    private async Task<HttpClient> CreateClientWithCurrentUserAsync()
    {
        await _factory.ResetDatabaseAsync();

        var client = _factory.CreateClient();

        var user = await AppUserApiClient.CreateUserAsync(
            client,
            AppUserTestData.ValidUserName,
            AppUserTestData.ValidPassword,
            AppUserTestData.ValidSex,
            AppUserTestData.ValidPhoneNumber,
            AppUserTestData.ValidEmail);

        _factory.CurrentUser.Id = user.Id;

        return client;
    }

    private static Task<Contracts.ProductDtoJson> CreateProductAsync(HttpClient client)
    {
        return ProductApiClient.CreateProductAsync(
            client,
            ProductTestData.ValidProductCode,
            ProductTestData.ValidProductName,
            ProductTestData.ValidProductDescription,
            ProductTestData.ValidUnitPrice,
            ProductTestData.ValidCurrency);
    }
}
