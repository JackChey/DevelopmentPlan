using FluentAssertions;
using InprovePlan.Domain.Entities;
using InprovePlan.IntegrationTests.Infrastructure;
using InprovePlan.UserCase.AppOrders.Commands;
using Instructure.Interfaces;
using Instructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InprovePlan.IntegrationTests.UseCases.AppOrders;

/// <summary>
/// 订单命令处理器集成测试。
/// </summary>
[Collection(Infrastructure.IntegrationTestCollection.Name)]
public sealed class AppOrderCommandHandlerTests
{
    private readonly Infrastructure.MySqlTestFixture _fixture;

    public AppOrderCommandHandlerTests(Infrastructure.MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// 测试场景：当前用户对启用商品下单。
    /// 预期结果：创建订单成功，并保存商品快照。
    /// </summary>
    [Fact]
    public async Task Create_ShouldCreateOrderWithProductSnapshot()
    {
        await _fixture.ResetDatabaseAsync();

        var user = await SeedUserAsync("order_create_user", "order_create_user@example.com", "13900005001");
        var product = await SeedProductAsync("IT-ORDER-PRODUCT-001", "订单商品001", AppProductStatus.Enable);
        var currentUser = new TestCurrentUser { Id = user.Id };

        var handler = new CreateAppOrderCommandHandler(
            new EfRepository<AppOrder>(_fixture.DbContext),
            new EfReadRepository<Product>(_fixture.DbContext),
            currentUser);

        var result = await handler.Handle(new CreateAppOrderCommand(product.Id, 2.345m, 10001L), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductCode.Should().Be(product.ProductCode);
        result.Value.ProductName.Should().Be(product.ProductName);
        result.Value.UnitPrice.Should().Be(product.UnitPrice);
        result.Value.Currency.Should().Be(product.Currency);
        result.Value.OrderStatus.Should().Be(AppOrderStatus.Addition);
    }

    /// <summary>
    /// 测试场景：对 Void 商品下单。
    /// 预期结果：返回失败，不创建订单。
    /// </summary>
    [Fact]
    public async Task Create_ShouldFail_WhenProductIsVoid()
    {
        await _fixture.ResetDatabaseAsync();

        var user = await SeedUserAsync("order_void_user", "order_void_user@example.com", "13900005002");
        var product = await SeedProductAsync("IT-ORDER-PRODUCT-VOID", "作废商品", AppProductStatus.Void);

        var handler = new CreateAppOrderCommandHandler(
            new EfRepository<AppOrder>(_fixture.DbContext),
            new EfReadRepository<Product>(_fixture.DbContext),
            new TestCurrentUser { Id = user.Id });

        var result = await handler.Handle(new CreateAppOrderCommand(product.Id, 1.000m, 10002L), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();

        var count = await _fixture.DbContext.Set<AppOrder>().CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(0);
    }

    /// <summary>
    /// 测试场景：修改自己的待支付订单。
    /// 预期结果：数量、地址、总金额更新成功。
    /// </summary>
    [Fact]
    public async Task Update_ShouldUpdateOwnAdditionOrder()
    {
        await _fixture.ResetDatabaseAsync();

        var user = await SeedUserAsync("order_update_user", "order_update_user@example.com", "13900005003");
        var product = await SeedProductAsync("IT-ORDER-PRODUCT-UPDATE", "订单修改商品", AppProductStatus.Enable);
        var order = await SeedOrderAsync(user, product, "O202606110001", 1.000m, 20001L, AppOrderStatus.Addition);

        var handler = new UpdateAppOrderCommandHandler(
            new EfRepository<AppOrder>(_fixture.DbContext),
            new TestCurrentUser { Id = user.Id });

        var result = await handler.Handle(new UpdateAppOrderCommand(order.Id, 3.125m, 20002L), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Quantity.Should().Be(3.125m);
        result.Value.AddressId.Should().Be(20002L);
        result.Value.TotalAmount.Should().Be(product.UnitPrice * 3.125m);
    }

    /// <summary>
    /// 测试场景：修改非 Addition 状态订单。
    /// 预期结果：返回失败，不允许修改。
    /// </summary>
    [Fact]
    public async Task Update_ShouldFail_WhenOrderStatusIsNotAddition()
    {
        await _fixture.ResetDatabaseAsync();

        var user = await SeedUserAsync("order_paid_user", "order_paid_user@example.com", "13900005004");
        var product = await SeedProductAsync("IT-ORDER-PRODUCT-PAID", "已支付订单商品", AppProductStatus.Enable);
        var order = await SeedOrderAsync(user, product, "O202606110002", 1.000m, 30001L, AppOrderStatus.Paid);

        var handler = new UpdateAppOrderCommandHandler(
            new EfRepository<AppOrder>(_fixture.DbContext),
            new TestCurrentUser { Id = user.Id });

        var result = await handler.Handle(new UpdateAppOrderCommand(order.Id, 2.000m, 30002L), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    /// <summary>
    /// 测试场景：删除自己的待支付订单。
    /// 预期结果：订单被删除。
    /// </summary>
    [Fact]
    public async Task Delete_ShouldRemoveOwnAdditionOrder()
    {
        await _fixture.ResetDatabaseAsync();

        var user = await SeedUserAsync("order_delete_user", "order_delete_user@example.com", "13900005005");
        var product = await SeedProductAsync("IT-ORDER-PRODUCT-DELETE", "订单删除商品", AppProductStatus.Enable);
        var order = await SeedOrderAsync(user, product, "O202606110003", 1.000m, 40001L, AppOrderStatus.Addition);

        var handler = new DeleteAppOrderCommandHandler(
            new EfRepository<AppOrder>(_fixture.DbContext),
            new TestCurrentUser { Id = user.Id });

        var result = await handler.Handle(new DeleteAppOrderCommand(order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var exists = await _fixture.DbContext.Set<AppOrder>()
            .AnyAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);

        exists.Should().BeFalse();
    }

    /// <summary>
    /// 测试场景：修改订单状态。
    /// 预期结果：OrderStatus 更新为目标状态。
    /// </summary>
    [Fact]
    public async Task ChangeStatus_ShouldUpdateOrderStatus()
    {
        await _fixture.ResetDatabaseAsync();

        var user = await SeedUserAsync("order_status_user", "order_status_user@example.com", "13900005006");
        var product = await SeedProductAsync("IT-ORDER-PRODUCT-STATUS", "订单状态商品", AppProductStatus.Enable);
        var order = await SeedOrderAsync(user, product, "O202606110004", 1.000m, 50001L, AppOrderStatus.Addition);

        var handler = new ChangeAppOrderStatusCommandHandler(new EfRepository<AppOrder>(_fixture.DbContext));

        var result = await handler.Handle(
            new ChangeAppOrderStatusCommand(order.Id, AppOrderStatus.Paid),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OrderStatus.Should().Be(AppOrderStatus.Paid);
    }

    private async Task<AppUser> SeedUserAsync(string userName, string email, string phone)
    {
        var user = Infrastructure.TestEntityFactory.CreateUser(_fixture.IdGenerator, _fixture.PasswordHasher, userName, email, phone);

        await _fixture.DbContext.Set<AppUser>().AddAsync(user, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return user;
    }

    private async Task<Product> SeedProductAsync(string code, string name, AppProductStatus status)
    {
        var product = new Product
        {
            Id = _fixture.IdGenerator.NewId(),
            ProductCode = code,
            ProductName = name,
            ProductDescription = "商品描述。",
            ProductTypeId = 1,
            ProductStatus = status,
            UnitPrice = 99.99m,
            Currency = "CNY",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _fixture.DbContext.Set<Product>().AddAsync(product, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return product;
    }

    private async Task<AppOrder> SeedOrderAsync(
        AppUser user,
        Product product,
        string orderNo,
        decimal quantity,
        long addressId,
        AppOrderStatus status)
    {
        var order = TestEntityFactory.CreateOrder(
        _fixture.IdGenerator,
        user,
        product,
        orderNo,
        quantity,
        addressId,
        status);

        await _fixture.DbContext.Set<AppOrder>().AddAsync(
            order,
            TestContext.Current.CancellationToken);

        await _fixture.DbContext.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        return order;
    }

    private sealed class TestCurrentUser : IUser
    {
        public long? Id { get; set; }
    }
}