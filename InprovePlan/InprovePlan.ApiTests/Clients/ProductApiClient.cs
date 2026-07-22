using InprovePlan.ApiTests.Contracts;
using InprovePlan.Domain.Entities;

namespace InprovePlan.ApiTests.Clients;

public static class ProductApiClient
{
    public static async Task<ProductDtoJson> CreateProductAsync(
        HttpClient client,
        string productCode,
        string productName,
        string productDescription,
        decimal unitPrice,
        string currency,
        int productTypeId = 1)
    {
        var response = await client.PostAsJsonAsync(
            "/api/Product",
            new
            {
                ProductCode = productCode,
                ProductName = productName,
                ProductDescription = productDescription,
                ProductTypeId = productTypeId,
                UnitPrice = unitPrice,
                Currency = currency
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<ProductDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }

    public static async Task<ProductDtoJson> UpdateProductAsync(
        HttpClient client,
        long id,
        string productName,
        string productDescription,
        int productTypeId,
        AppProductStatus productStatus,
        decimal unitPrice,
        string currency)
    {
        var response = await client.PutAsJsonAsync(
            $"/api/Product/{id}",
            new
            {
                ProductName = productName,
                ProductDescription = productDescription,
                ProductTypeId = productTypeId,
                ProductStatus = productStatus,
                UnitPrice = unitPrice,
                Currency = currency
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<ProductDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }

    public static async Task DeleteProductAsync(HttpClient client, long id)
    {
        var response = await client.DeleteAsync(
            $"/api/Product/{id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<object>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
    }

    public static async Task<ProductDtoJson> GetProductByIdAsync(HttpClient client, long id)
    {
        var response = await client.GetAsync(
            $"/api/Product/{id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<ProductDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }

    public static async Task<PagedResultJson<ProductDtoJson>> GetProductsPagedAsync(
        HttpClient client,
        string? keyword = null,
        int? productTypeId = null,
        AppProductStatus? productStatus = null,
        bool includeVoid = false)
    {
        var query = $"/api/Product?pageIndex=1&pageSize=10&sortBy=createdAt&sortDirection=desc&includeVoid={includeVoid.ToString().ToLowerInvariant()}";

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query += $"&keyword={Uri.EscapeDataString(keyword)}";
        }

        if (productTypeId.HasValue)
        {
            query += $"&productTypeId={productTypeId.Value}";
        }

        if (productStatus.HasValue)
        {
            query += $"&productStatus={(int)productStatus.Value}";
        }

        var response = await client.GetAsync(query, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<PagedResultJson<ProductDtoJson>>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }
}
