using InprovePlan.ApiTests.Contracts;
using InprovePlan.Domain.Entities;

namespace InprovePlan.ApiTests.Clients;

public static class AppOrderApiClient
{
    public static async Task<AppOrderDtoJson> CreateOrderAsync(
        HttpClient client,
        long productId,
        decimal quantity,
        long addressId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/AppOrder",
            new
            {
                ProductId = productId,
                Quantity = quantity,
                AddressId = addressId
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppOrderDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }

    public static async Task<AppOrderDtoJson> CreateOrderWithIdempotencyAsync(
        HttpClient client,
        long productId,
        decimal quantity,
        long addressId,
        string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/AppOrder/CreateWithIdempotency")
        {
            Content = JsonContent.Create(new
            {
                ProductId = productId,
                Quantity = quantity,
                AddressId = addressId
            })
        };

        request.Headers.Add("Idempotency-Key", idempotencyKey);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppOrderDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }

    public static async Task<AppOrderDtoJson> UpdateOrderAsync(
        HttpClient client,
        long id,
        decimal quantity,
        long addressId)
    {
        var response = await client.PutAsJsonAsync(
            $"/api/AppOrder/{id}",
            new
            {
                Quantity = quantity,
                AddressId = addressId
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppOrderDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }

    public static async Task<AppOrderDtoJson> ChangeOrderStatusAsync(
        HttpClient client,
        long id,
        AppOrderStatus orderStatus,
        string updateReason,
        string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/AppOrder/{id}/status")
        {
            Content = JsonContent.Create(new
            {
                OrderStatus = orderStatus,
                UpdateReason = updateReason
            })
        };

        request.Headers.Add("Idempotency-Key", idempotencyKey);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppOrderDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }

    public static async Task DeleteOrderAsync(HttpClient client, long id)
    {
        var response = await client.DeleteAsync(
            $"/api/AppOrder/{id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<object>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
    }

    public static async Task<AppOrderDtoJson> GetOrderByIdAsync(HttpClient client, long id)
    {
        var response = await client.GetAsync(
            $"/api/AppOrder/{id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppOrderDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }

    public static async Task<PagedResultJson<AppOrderDtoJson>> GetOrdersPagedAsync(
        HttpClient client,
        string? keyword = null,
        long? userId = null,
        long? productId = null,
        AppOrderStatus? orderStatus = null)
    {
        var query = "/api/AppOrder?pageIndex=1&pageSize=10&sortBy=createdAt&sortDirection=desc";

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query += $"&keyword={Uri.EscapeDataString(keyword)}";
        }

        if (userId.HasValue)
        {
            query += $"&userId={userId.Value}";
        }

        if (productId.HasValue)
        {
            query += $"&productId={productId.Value}";
        }

        if (orderStatus.HasValue)
        {
            query += $"&orderStatus={(int)orderStatus.Value}";
        }

        var response = await client.GetAsync(query, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<PagedResultJson<AppOrderDtoJson>>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }
}
