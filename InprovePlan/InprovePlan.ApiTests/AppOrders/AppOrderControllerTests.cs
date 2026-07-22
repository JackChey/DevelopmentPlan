using InprovePlan.ApiTests.Clients;
using InprovePlan.ApiTests.Infrastructure;
using InprovePlan.ApiTests.TestData;
using InprovePlan.Domain.Entities;

namespace InprovePlan.ApiTests.AppOrders;

[Collection(WebApiTestCollection.Name)]
public sealed class AppOrderControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AppOrderControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WhenProductExistsAndCurrentUserExists_ShouldCreateOrder()
    {
        var client = await CreateClientWithCurrentUserAsync();
        var product = await CreateProductAsync(client);

        var order = await AppOrderApiClient.CreateOrderAsync(
            client,
            product.Id,
            AppOrderTestData.ValidQuantity,
            AppOrderTestData.ValidAddressId);

        order.ProductId.Should().Be(product.Id);
        order.ProductCode.Should().Be(ProductTestData.ValidProductCode.ToUpperInvariant());
        order.Quantity.Should().Be(AppOrderTestData.ValidQuantity);
        order.OrderStatus.Should().Be((int)AppOrderStatus.Addition);
    }

    [Fact]
    public async Task CreateWithIdempotency_WhenProductExistsAndCurrentUserExists_ShouldCreateOrder()
    {
        var client = await CreateClientWithCurrentUserAsync();
        var product = await CreateProductAsync(client);

        var order = await AppOrderApiClient.CreateOrderWithIdempotencyAsync(
            client,
            product.Id,
            3.000m,
            70001L,
            Guid.NewGuid().ToString("N"));

        order.ProductId.Should().Be(product.Id);
        order.Quantity.Should().Be(3.000m);
        order.AddressId.Should().Be(70001L);
    }

    [Fact]
    public async Task Update_WhenOrderExistsAndCurrentUserOwnsOrder_ShouldUpdateOrder()
    {
        var client = await CreateClientWithCurrentUserAsync();
        var product = await CreateProductAsync(client);
        var order = await AppOrderApiClient.CreateOrderAsync(client, product.Id, 1.000m, 60001L);

        var updated = await AppOrderApiClient.UpdateOrderAsync(
            client,
            order.Id,
            5.000m,
            60002L);

        updated.Id.Should().Be(order.Id);
        updated.Quantity.Should().Be(5.000m);
        updated.AddressId.Should().Be(60002L);
    }

    [Fact]
    public async Task ChangeStatus_WhenOrderExists_ShouldUpdateOrderStatus()
    {
        var client = await CreateClientWithCurrentUserAsync();
        var product = await CreateProductAsync(client);
        var order = await AppOrderApiClient.CreateOrderAsync(client, product.Id, 1.000m, 60001L);

        var updated = await AppOrderApiClient.ChangeOrderStatusAsync(
            client,
            order.Id,
            AppOrderStatus.Paid,
            "paid by api test",
            Guid.NewGuid().ToString("N"));

        updated.Id.Should().Be(order.Id);
        updated.OrderStatus.Should().Be((int)AppOrderStatus.Paid);
    }

    [Fact]
    public async Task Delete_WhenOrderExistsAndCurrentUserOwnsOrder_ShouldDeleteOrder()
    {
        var client = await CreateClientWithCurrentUserAsync();
        var product = await CreateProductAsync(client);
        var order = await AppOrderApiClient.CreateOrderAsync(client, product.Id, 1.000m, 60001L);

        await AppOrderApiClient.DeleteOrderAsync(client, order.Id);
    }

    [Fact]
    public async Task GetById_WhenOrderExistsAndCurrentUserOwnsOrder_ShouldReturnOrder()
    {
        var client = await CreateClientWithCurrentUserAsync();
        var product = await CreateProductAsync(client);
        var order = await AppOrderApiClient.CreateOrderAsync(client, product.Id, 1.000m, 60001L);

        var found = await AppOrderApiClient.GetOrderByIdAsync(client, order.Id);

        found.Id.Should().Be(order.Id);
        found.ProductId.Should().Be(product.Id);
    }

    [Fact]
    public async Task GetPaged_WhenOrdersExist_ShouldReturnPagedOrders()
    {
        var client = await CreateClientWithCurrentUserAsync();
        var product = await CreateProductAsync(client);
        var first = await AppOrderApiClient.CreateOrderAsync(client, product.Id, 1.000m, 60001L);
        await AppOrderApiClient.CreateOrderAsync(client, product.Id, 2.000m, 60002L);

        var page = await AppOrderApiClient.GetOrdersPagedAsync(
            client,
            userId: first.UserId,
            productId: product.Id,
            orderStatus: AppOrderStatus.Addition);

        page.Total.Should().Be(2);
        page.Items.Should().OnlyContain(order => order.ProductId == product.Id);
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
