using InprovePlan.IntegrationTests.Builders;
using InprovePlan.IntegrationTests.DataSeeders;
using InprovePlan.IntegrationTests.Infrastructure;
using InprovePlan.IntegrationTests.TestData;
using InprovePlan.IntegrationTests.TestDoubles;
using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.AppOrders;
using InprovePlan.UserCase.AppOrders.Queries;
using Instructure.Caching;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.IntegrationTests.AppOrders;

/// <summary>
/// 应用订单查询处理器的集成测试类。
/// 该类专门用于测试涉及 MySQL 数据库读取和 Redis 缓存交互的查询逻辑。
/// 使用 [Collection] 特性确保测试在指定的集成测试集合中串行执行，以隔离数据库和缓存状态。
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class AppOrderQueryHandlerTests
{
    // MySQL 测试夹具，用于管理关系型数据库的连接、重置和上下文创建
    private readonly MySqlTestFixture _mysqlFixture;

    // Redis 测试夹具，用于管理缓存服务的连接、重置和缓存操作
    private readonly RedisTestFixture _redisFixture;

    /// <summary>
    /// 构造函数，通过依赖注入获取 MySQL 和 Redis 的测试夹具实例。
    /// </summary>
    /// <param name="mysqlFixture">MySQL 数据库测试环境配置</param>
    /// <param name="redisFixture">Redis 缓存测试环境配置</param>
    public AppOrderQueryHandlerTests(MySqlTestFixture mysqlFixture, RedisTestFixture redisFixture)
    {
        this._mysqlFixture = mysqlFixture;
        this._redisFixture = redisFixture;
    }

    /// <summary>
    /// 测试场景：当订单存在且当前用户拥有该订单时，执行获取订单详情查询。
    /// 预期结果：
    /// 1. 查询成功返回订单详情。
    /// 2. 验证缓存穿透/写入逻辑：查询前缓存为空，查询后缓存中应包含该订单数据。
    /// </summary>
    [Fact]
    public async Task Handle_WhenOrderExistsAndCurrentUserOwnsOrder_ShouldReturnOrder()
    {
        // 1. 环境重置：分别重置 MySQL 数据库和 Redis 缓存，确保测试环境干净且无残留数据
        await _mysqlFixture.ResetDatabaseAsync();
        await _redisFixture.ResetDatabaseAsync();

        // 2. 数据库上下文初始化：创建一个新的 DbContext 实例用于数据种子化
        await using var dbContext = _mysqlFixture.CreateDbContext();

        // 3. 测试数据构建：使用 Builder 模式创建订单、用户和产品实体
        // 构建订单：指定有效的产品 ID 和用户 ID
        var order = new AppOrderBuilder(_mysqlFixture.IdGenerator)
            .WithProductId(ProductTestData.ValidProductId)
            .WithUserId(AppUserTestData.ValidUserId)
            .Build();

        // 构建用户：确保用户 ID 与订单中的用户 ID 一致，模拟订单所有者
        var user = new AppUserBuilder(_mysqlFixture.IdGenerator, _mysqlFixture.PasswordHasher)
            .WithUserId(AppUserTestData.ValidUserId)
            .Build();

        // 构建产品：确保产品 ID 与订单中的产品 ID 一致
        var product = new ProductBuilder(_mysqlFixture.IdGenerator)
            .WithProductId(ProductTestData.ValidProductId)
            .Build();

        // 4. 仓库初始化：创建订单只读仓库，用于后续查询处理器中的数据访问
        var orderRepository = new EfReadRepository<AppOrder>(dbContext);

        // 5. 数据种子化：将构建的用户、产品和订单数据持久化到 MySQL 数据库中
        var dataSeeder = new AppDbContextDataSeeder(dbContext);
        await dataSeeder.SeedAppUserAsync(user);
        await dataSeeder.SeedProductAsync(product);
        await dataSeeder.SeedAppOrderAsync(order);

        // 6. 模拟当前用户上下文：设置当前用户 ID 为订单的所有者 ID，用于权限验证
        var currentUser = new FakeCurrentUser() { Id = order.UserId };

        // 7. 缓存键构建：根据模块、名称和订单 ID 生成唯一的 Redis 缓存键
        var cacheKey = _redisFixture.CacheKeyBuilder.Build(module: "order",
            name: "detail",
            order.Id);

        // 8. 前置缓存断言：在执行查询前，验证 Redis 中不存在该订单的缓存数据
        // 确保测试的是“缓存未命中 -> 查询数据库 -> 写入缓存”的完整流程
        var beforeCache = await _redisFixture.AppCache.GetAsync<AppOrderDto>(cacheKey, TestContext.Current.CancellationToken);
        beforeCache.Should().BeNull();

        // 9. 构建查询请求：创建根据 ID 获取订单详情的查询对象
        var query = new GetAppOrderByIdQuery(order.Id);

        // 10. 实例化查询处理器：注入订单仓库、Redis 缓存服务、缓存键构建器和当前用户上下文
        var queryHandler = new GetAppOrderByIdQueryHandler(
            orderRepository,
            _redisFixture.AppCache,
            _redisFixture.CacheKeyBuilder,
            currentUser);

        // 11. 执行查询：调用 Handle 方法处理查询请求
        var result = await queryHandler.Handle(query, TestContext.Current.CancellationToken);

        // 12. 结果断言：验证查询执行状态和返回数据
        // 验证状态为成功 (Ok)
        result.Status.Should().Be(ResultStatus.Ok);

        // 验证返回的订单对象不为空
        result.Value.Should().NotBeNull();

        // 验证返回的订单 ID 与请求的订单 ID 一致
        result.Value.Id.Should().Be(order.Id);

        // 13. 后置缓存断言：验证查询执行后，Redis 中已正确写入订单缓存
        var cacheOrder = await _redisFixture.AppCache.GetAsync<AppOrderDto>(cacheKey, cancellationToken: TestContext.Current.CancellationToken);

        // 验证缓存对象不为空，证明缓存写入逻辑生效
        cacheOrder.Should().NotBeNull();

        // 验证缓存中的订单 ID 与预期一致，确保缓存数据的准确性
        cacheOrder.Id.Should().Be(order.Id);
    }
}

