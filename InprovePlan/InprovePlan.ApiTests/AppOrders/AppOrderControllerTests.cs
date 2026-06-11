using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InprovePlan.ApiTests.Infrastructure;
using InprovePlan.Domain.Entities;
using Instructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InprovePlan.ApiTests.AppOrders;

/// <summary>
/// 订单接口测试。
/// </summary>
public sealed class AppOrderControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AppOrderControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// 测试场景：登录用户对启用商品下单。
    /// 预期结果：返回成功，订单保存商品快照，状态为 Addition。
    /// </summary>
    [Fact]
    public async Task Create_ShouldReturnCreatedOrder()
    {
        await ResetAsync();

        var client = _factory.CreateClient();
        var user = await CreateUserAsync(client, "order_create_user", "13900004001", "order_create_user@example.com");
        _factory.CurrentUser.Id = user.Id;

        var product = await CreateProductAsync(client, "API-ORDER-PRODUCT-001", "api_order_product_001");

        var response = await client.PostAsJsonAsync(
            "/api/AppOrder",
            new
            {
                ProductId = product.Id,
                Quantity = 2.345m,
                AddressId = 10001L
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppOrderDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        body.Data!.ProductId.Should().Be(product.Id);
        body.Data.ProductCode.Should().Be("API-ORDER-PRODUCT-001");
        body.Data.ProductName.Should().Be("api_order_product_001");
        body.Data.Quantity.Should().Be(2.345m);
        body.Data.OrderStatus.Should().Be((int)AppOrderStatus.Addition);
        body.Data.AddressId.Should().Be(10001L);
    }

    /// <summary>
    /// 测试场景：修改待支付订单数量和地址。
    /// 预期结果：返回成功，Quantity、AddressId、TotalAmount 被更新。
    /// </summary>
    [Fact]
    public async Task Update_ShouldUpdateQuantityAndAddress()
    {
        await ResetAsync();

        var client = _factory.CreateClient();
        var user = await CreateUserAsync(client, "order_update_user", "13900004002", "order_update_user@example.com");
        _factory.CurrentUser.Id = user.Id;

        var product = await CreateProductAsync(client, "API-ORDER-PRODUCT-UPDATE", "api_order_product_update");
        var order = await CreateOrderAsync(client, product.Id, 1.000m, 20001L);

        var response = await client.PutAsJsonAsync(
            $"/api/AppOrder/{order.Id}",
            new
            {
                Quantity = 3.125m,
                AddressId = 20002L
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppOrderDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        body.Data!.Quantity.Should().Be(3.125m);
        body.Data.AddressId.Should().Be(20002L);
        body.Data.TotalAmount.Should().Be(312.46875m);
    }

    /// <summary>
    /// 测试场景：修改订单状态。
    /// 预期结果：OrderStatus 变为指定状态，例如 Paid。
    /// </summary>
    [Fact]
    public async Task ChangeStatus_ShouldUpdateOrderStatus()
    {
        await ResetAsync();

        var client = _factory.CreateClient();
        var user = await CreateUserAsync(client, "order_status_user", "13900004003", "order_status_user@example.com");
        _factory.CurrentUser.Id = user.Id;

        var product = await CreateProductAsync(client, "API-ORDER-PRODUCT-STATUS", "api_order_product_status");
        var order = await CreateOrderAsync(client, product.Id, 1.000m, 30001L);

        var response = await client.PutAsJsonAsync(
            $"/api/AppOrder/{order.Id}/status",
            new
            {
                OrderStatus = AppOrderStatus.Paid
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var entity = dbContext.Set<AppOrder>().Single(x => x.Id == order.Id);
        entity.OrderStatus.Should().Be(AppOrderStatus.Paid);
    }

    /// <summary>
    /// 测试场景：删除待支付订单。
    /// 预期结果：订单被物理删除。
    /// </summary>
    [Fact]
    public async Task Delete_ShouldRemoveAdditionOrder()
    {
        await ResetAsync();

        var client = _factory.CreateClient();
        var user = await CreateUserAsync(client, "order_delete_user", "13900004004", "order_delete_user@example.com");
        _factory.CurrentUser.Id = user.Id;

        var product = await CreateProductAsync(client, "API-ORDER-PRODUCT-DELETE", "api_order_product_delete");
        var order = await CreateOrderAsync(client, product.Id, 1.000m, 40001L);

        var response = await client.DeleteAsync(
            $"/api/AppOrder/{order.Id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var exists = dbContext.Set<AppOrder>().Any(x => x.Id == order.Id);
        exists.Should().BeFalse();
    }

    /// <summary>
    /// 测试场景：查询当前用户自己的订单。
    /// 预期结果：返回订单详情。
    /// </summary>
    [Fact]
    public async Task GetById_ShouldReturnOwnOrder()
    {
        await ResetAsync();

        var client = _factory.CreateClient();
        var user = await CreateUserAsync(client, "order_get_user", "13900004005", "order_get_user@example.com");
        _factory.CurrentUser.Id = user.Id;

        var product = await CreateProductAsync(client, "API-ORDER-PRODUCT-GET", "api_order_product_get");
        var order = await CreateOrderAsync(client, product.Id, 2.000m, 50001L);

        var response = await client.GetAsync(
            $"/api/AppOrder/{order.Id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppOrderDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        body.Data!.Id.Should().Be(order.Id);
        body.Data.ProductId.Should().Be(product.Id);
        body.Data.UserId.Should().Be(user.Id);
    }

    /// <summary>
    /// 测试场景：分页查询订单。
    /// 预期结果：按 userId、productId、orderStatus、时间范围过滤。
    /// </summary>
    [Fact]
    public async Task GetPaged_ShouldReturnPagedOrders()
    {
        await ResetAsync();

        var client = _factory.CreateClient();

        var firstUser = await CreateUserAsync(client, "order_page_user_01", "13900004006", "order_page_user_01@example.com");
        _factory.CurrentUser.Id = firstUser.Id;

        var product = await CreateProductAsync(client, "API-ORDER-PRODUCT-PAGE", "api_order_product_page");
        await CreateOrderAsync(client, product.Id, 1.000m, 60001L);
        await CreateOrderAsync(client, product.Id, 2.000m, 60002L);

        var response = await client.GetAsync(
            $"/api/AppOrder?pageIndex=1&pageSize=10&sortBy=createdAt&sortDirection=desc&userId={firstUser.Id}&productId={product.Id}&orderStatus=0",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<PagedResultJson<AppOrderDtoJson>>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        body.Data!.Total.Should().Be(2);
        body.Data.Items.Should().HaveCount(2);
        body.Data.Items.Should().OnlyContain(x => x.UserId == firstUser.Id);
        body.Data.Items.Should().OnlyContain(x => x.ProductId == product.Id);
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

    private static async Task<ProductDtoJson> CreateProductAsync(HttpClient client, string productCode, string productName)
    {
        var response = await client.PostAsJsonAsync(
            "/api/Product",
            new
            {
                ProductCode = productCode,
                ProductName = productName,
                ProductDescription = "符合 ProductConfiguration 长度要求的商品描述。",
                ProductTypeId = 1,
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

    private static async Task<AppOrderDtoJson> CreateOrderAsync(HttpClient client, long productId, decimal quantity, long addressId)
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
    }

    private sealed class AppOrderDtoJson
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public long UserId { get; set; }
        public int OrderStatus { get; set; }
        public long AddressId { get; set; }
    }

    private sealed class PagedResultJson<T>
    {
        public long Total { get; set; }
        public int Count { get; set; }
        public List<T> Items { get; set; } = [];
    }
}