using FluentAssertions;
using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.AppOrders.Queries;
using InprovePlan.UserCase.Caching;
using Instructure.Caching;
using Instructure.Interfaces;
using Instructure.Paging;
using Instructure.Repositories;
using Instructure.Sorting;
using Microsoft.AspNetCore.Http;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace InprovePlan.IntegrationTests.UseCases.AppOrders;

/// <summary>
/// 订单查询处理器集成测试。
/// </summary>
[Collection(Infrastructure.IntegrationTestCollection.Name)]
public sealed class AppOrderQueryHandlerTests
{
    private readonly Infrastructure.MySqlTestFixture _fixture;

    public AppOrderQueryHandlerTests(Infrastructure.MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// 测试场景：当前用户查询自己的订单。
    /// 预期结果：返回订单详情。
    /// </summary>
    [Fact(Skip ="使用缓存暂时搁置测试,后续完善")]
    public async Task GetById_ShouldReturnOwnOrder()
    {
        //await _fixture.ResetDatabaseAsync();

        //var user = await SeedUserAsync("order_query_user", "order_query_user@example.com", "13900006001");
        //var product = await SeedProductAsync("IT-ORDER-QUERY-PRODUCT", "订单查询商品");
        //var order = await SeedOrderAsync(user, product, "O202606110101", 1.000m, 10001L, AppOrderStatus.Addition);

        //var handler = new GetAppOrderByIdQueryHandler(
        //    new AppCache(IFusionCache _cache, CacheOptions _options, ILogger < AppCache > _logger, HttpContext ctx),
        //    new EfReadRepository<AppOrder>(_fixture.DbContext),
        //    new TestCurrentUser { Id = user.Id });

        //var result = await handler.Handle(new GetAppOrderByIdQuery(order.Id), CancellationToken.None);

        //result.IsSuccess.Should().BeTrue();
        //result.Value!.Id.Should().Be(order.Id);
        //result.Value.UserId.Should().Be(user.Id);
    }

    /// <summary>
    /// 测试场景：其他用户查询订单。
    /// 预期结果：返回失败，防止越权访问。
    /// </summary>
    [Fact(Skip ="使用缓存暂时搁置测试,后续完善")]
    public async Task GetById_ShouldFail_WhenOrderBelongsToOtherUser()
    {
        //await _fixture.ResetDatabaseAsync();

        //var owner = await SeedUserAsync("order_owner", "order_owner@example.com", "13900006002");
        //var other = await SeedUserAsync("order_other", "order_other@example.com", "13900006003");
        //var product = await SeedProductAsync("IT-ORDER-OTHER-PRODUCT", "越权查询商品");
        //var order = await SeedOrderAsync(owner, product, "O202606110102", 1.000m, 10002L, AppOrderStatus.Addition);

        //var handler = new GetAppOrderByIdQueryHandler(
        //    new EfReadRepository<AppOrder>(_fixture.DbContext),
        //    new TestCurrentUser { Id = other.Id });

        //var result = await handler.Handle(new GetAppOrderByIdQuery(order.Id), CancellationToken.None);

        //result.IsSuccess.Should().BeFalse();
    }

    /// <summary>
    /// 测试场景：分页查询订单。
    /// 预期结果：按 userId、productId、orderStatus、startTime、endTime 过滤。
    /// </summary>
    [Fact]
    public async Task GetPaged_ShouldReturnMatchedOrders()
    {
        await _fixture.ResetDatabaseAsync();

        var user = await SeedUserAsync("order_page_user", "order_page_user@example.com", "13900006004");
        var product = await SeedProductAsync("IT-ORDER-PAGE-PRODUCT", "订单分页商品");

        var now = DateTimeOffset.UtcNow;
        await SeedOrderAsync(user, product, "O202606110201", 1.000m, 20001L, AppOrderStatus.Addition, now.AddMinutes(-10));
        await SeedOrderAsync(user, product, "O202606110202", 2.000m, 20002L, AppOrderStatus.Addition, now.AddMinutes(-5));
        await SeedOrderAsync(user, product, "O202606110203", 3.000m, 20003L, AppOrderStatus.Paid, now.AddMinutes(-1));

        var handler = new GetAppOrdersPagedQueryHandler(new EfReadRepository<AppOrder>(_fixture.DbContext));

        var result = await handler.Handle(
            new GetAppOrdersPagedQuery(
                new Pagination { PageIndex = 1, PageSize = 10 },
                new SortQuery { SortBy = "createdAt", SortDirection = "desc" },
                "O2026061102",
                user.Id,
                product.Id,
                AppOrderStatus.Addition,
                now.AddHours(-1),
                now.AddHours(1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(2);
        result.Value.Items.Should().OnlyContain(x => x.UserId == user.Id);
        result.Value.Items.Should().OnlyContain(x => x.ProductId == product.Id);
        result.Value.Items.Should().OnlyContain(x => x.OrderStatus == AppOrderStatus.Addition);
    }

    private async Task<AppUser> SeedUserAsync(string userName, string email, string phone)
    {
        var user = Infrastructure.TestEntityFactory.CreateUser(_fixture.IdGenerator, _fixture.PasswordHasher, userName, email, phone);

        await _fixture.DbContext.Set<AppUser>().AddAsync(user, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return user;
    }

    private async Task<Product> SeedProductAsync(string code, string name)
    {
        var product = new Product
        {
            Id = _fixture.IdGenerator.NewId(),
            ProductCode = code,
            ProductName = name,
            ProductDescription = "商品描述。",
            ProductTypeId = 1,
            ProductStatus = AppProductStatus.Enable,
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
        AppOrderStatus status,
        DateTimeOffset? occurredTime = null)
    {
        var order = new AppOrder
        {
            Id = _fixture.IdGenerator.NewId(),
            OrderNo = orderNo,
            ProductId = product.Id,
            ProductName = product.ProductName,
            ProductCode = product.ProductCode,
            Currency = product.Currency,
            UnitPrice = product.UnitPrice,
            Quantity = quantity,
            UserId = user.Id,
            OccurredTime = occurredTime ?? DateTimeOffset.UtcNow,
            OrderStatus = status,
            Cancelled = false,
            AddressId = addressId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        order.RecalculateTotalAmount();

        await _fixture.DbContext.Set<AppOrder>().AddAsync(order, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return order;
    }

    private sealed class TestCurrentUser : IUser
    {
        public long? Id { get; set; }
    }
}