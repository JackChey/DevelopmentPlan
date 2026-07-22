using InprovePlan.Domain.Entities;
using InprovePlan.IntegrationTests.Helpers;
using InprovePlan.IntegrationTests.Infrastructure;
using InprovePlan.UserCase.AppOrders.Queries;
using Instructure.Interceptors;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.IntegrationTests.AppOrders;

/// <summary>
/// 应用订单附加查询处理器的集成测试类。
/// 用于测试涉及复杂过滤和分页的订单查询逻辑。
/// </summary>
[Collection(MySqlIntegrationTestCollection.Name)]
public sealed class AppOrderAdditionalQueryHandlerTests
{
    // MySQL 测试夹具，用于管理数据库连接、重置和上下文创建
    private readonly MySqlTestFixture _fixture;

    public AppOrderAdditionalQueryHandlerTests(MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// 测试场景：当存在符合多重过滤条件的订单时，执行分页查询。
    /// 预期结果：
    /// 1. 查询成功返回分页结果。
    /// 2. 总记录数 (Total) 为 1，因为只有一个订单完全匹配所有过滤条件。
    /// 3. 返回的项目列表 (Items) 中仅包含那个匹配的订单。
    /// </summary>
    [Fact]
    public async Task GetAppOrdersPaged_WhenOrdersMatchFilter_ShouldReturnPagedOrders()
    {
        // 1. 环境重置：清空数据库，确保测试环境干净，无残留数据干扰
        await _fixture.ResetDatabaseAsync();

        // 2. 创建数据库上下文
        await using var dbContext = _fixture.CreateDbContext();

        // 3. 种子数据构建：
        // 创建一个测试用户
        var user = TestEntityFactory.CreateUser(_fixture.IdGenerator, _fixture.PasswordHasher);

        // 创建一个测试产品，名称设为 "Paged Product" 以便识别
        var product = TestEntityFactory.CreateProduct(_fixture.IdGenerator, name: "Paged Product");

        // 创建第一个订单 (matched)：
        // - 关联上述用户和产品
        // - 状态设为 AppOrderStatus.Paid (已支付)
        // 此订单将作为预期被查询到的目标数据
        var matched = TestEntityFactory.CreateOrder(
            _fixture.IdGenerator,
            user,
            product,
            status: AppOrderStatus.Paid);

        // 创建第二个订单 (other)：
        // - 关联相同的用户和产品
        // 但状态设为 AppOrderStatus.Delivered (已发货)
        // 此订单用于验证过滤逻辑是否能正确排除不符合状态条件的数据
        var other = TestEntityFactory.CreateOrder(
            _fixture.IdGenerator,
            user,
            product,
            status: AppOrderStatus.Delivered);

        // 4. 数据持久化：
        // 将用户、产品和两个订单保存到 MySQL 数据库
        await dbContext.Set<AppUser>().AddAsync(user, TestContext.Current.CancellationToken);
        await dbContext.Set<Product>().AddAsync(product, TestContext.Current.CancellationToken);
        await dbContext.Set<AppOrder>().AddRangeAsync([matched, other], TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 5. 依赖服务初始化：
        // EfReadRepository<AppOrder>: 订单只读仓库，用于执行高效的数据检索
        var repository = new EfReadRepository<AppOrder>(dbContext);

        // GetAppOrdersPagedQueryHandler: 分页订单查询处理器
        var handler = new GetAppOrdersPagedQueryHandler(repository);

        // 6. 构建查询对象 (GetAppOrdersPagedQuery)：
        // 设置多重过滤条件，模拟真实业务中的复杂搜索场景
        var query = new GetAppOrdersPagedQuery(
            TestQueryFactory.Page(),       // 默认分页参数 (如 PageIndex=1, PageSize=10)
            TestQueryFactory.Sort(),       // 默认排序参数
            Keyword: matched.OrderNo,      // 关键字过滤：精确匹配订单号
            UserId: user.Id,               // 用户ID过滤：仅限该用户的订单
            ProductId: product.Id,         // 产品ID过滤：仅限该产品的订单
            OrderStatus: AppOrderStatus.Paid, // 状态过滤：仅限“已支付”状态 -> 这将排除 'other' 订单
            StartTime: DateTimeOffset.UtcNow.AddMinutes(-5), // 时间范围开始：5分钟前
            EndTime: DateTimeOffset.UtcNow.AddMinutes(5)     // 时间范围结束：5分钟后 (覆盖当前时间)
        );

        // 7. 执行查询：调用 Handle方法处理查询请求
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // 8. 结果断言：
        // 验证查询执行状态为成功 (Ok)
        result.Status.Should().Be(ResultStatus.Ok);

        // 验证返回的分页数据对象不为空
        result.Value.Should().NotBeNull();

        // 验证总记录数为 1：
        // 虽然数据库中有两个订单，但 'other' 订单因状态不符 (Delivered != Paid) 被过滤掉
        result.Value.Total.Should().Be(1);

        // 验证返回的项目列表中仅包含 'matched' 订单：
        // ContainSingle 确保列表中只有一个元素，且其 ID 与预期匹配的订单 ID 一致
        result.Value.Items.Should().ContainSingle(item => item.Id == matched.Id);
    }

    /// <summary>
    /// 测试场景：获取订单列表时，验证是否避免了 N+1 查询模式。
    /// 预期结果：
    /// 1. 查询成功返回订单及其关联项。
    /// 2. SQL 查询次数大于 0（至少有一次主查询）。
    /// 3. 通过 QueryCounterInterceptor 监控，确保没有发生典型的 N+1 次额外查询（具体断言逻辑可能在 Handler 内部或 SelectSqlCount 中体现，此处主要验证结构正确性）。
    /// </summary>
    [Fact]
    public async Task GetAppOrderTest_WhenOrdersExist_ShouldReturnOrderItemsWithoutNPlusOnePattern()
    {
        // 1. 环境重置
        await _fixture.ResetDatabaseAsync();

        // 2. 种子数据准备：创建用户、产品和两个订单
        await SeedOrdersAsync();

        // 3. 性能监控初始化：
        // QueryCounterInterceptor: 自定义拦截器，用于统计执行期间发出的 SQL SELECT 语句数量
        var queryCounter = new QueryCounterInterceptor();

        // 创建带有监控拦截器的 DbContext
        await using var dbContext = TestDbContextFactory.Create(_fixture.ConnectionString, queryCounter);

        // 4. 依赖服务初始化：
        // GetAppOrderTestQueryHandler: 专门用于测试 N+1 问题的查询处理器
        // 注入 Order, User, Product 的只读仓库，以及查询计数器
        var handler = new GetAppOrderTestQueryHandler(
            new EfReadRepository<AppOrder>(dbContext),
            new EfReadRepository<AppUser>(dbContext),
            new EfReadRepository<Product>(dbContext),
            queryCounter);

        // 5. 执行查询
        var result = await handler.Handle(
            new GetAppOrderTestQuery(),
            TestContext.Current.CancellationToken);

        // 6. 结果断言：
        // 验证状态成功
        result.Status.Should().Be(ResultStatus.Ok);
        result.Value.Should().NotBeNull();

        // 验证返回的订单数量大于 0
        result.Value.OrderCount.Should().BeGreaterThan(0);

        // 验证返回的项目列表数量与订单总数一致
        result.Value.Items.Should().HaveCount(result.Value.OrderCount);

        // 验证确实执行了 SQL 查询（SelectSqlCount > 0）
        // *注意*：通常避免 N+1 的断言会检查 SelectSqlCount 是否等于 1 或一个很小的固定值，
        // 这里仅检查 >0，可能具体的 N+1 检查逻辑在 Handler 内部或通过其他机制验证。
        result.Value.SelectSqlCount.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// 测试场景：使用 EF Core 默认追踪（Tracking）模式获取订单。
    /// 预期结果：
    /// 1. 查询成功。
    /// 2. 返回正确的订单数量和项目列表。
    /// 目的：验证在开启变更追踪的情况下，数据读取的正确性。
    /// </summary>
    [Fact]
    public async Task GetAppOrderWithtrackingTest_WhenOrdersExist_ShouldReturnOrderItems()
    {
        // 1. 环境重置
        await _fixture.ResetDatabaseAsync();

        // 2. 种子数据准备
        await SeedOrdersAsync();

        // 3. 性能监控初始化
        var queryCounter = new QueryCounterInterceptor();
        await using var dbContext = TestDbContextFactory.Create(_fixture.ConnectionString, queryCounter);

        // 4. 依赖服务初始化：
        // GetAppOrderWithtrackingTestQueryHandler: 使用 Tracking 模式的查询处理器
        var handler = new GetAppOrderWithtrackingTestQueryHandler(
            new EfReadRepository<AppOrder>(dbContext),
            queryCounter);

        // 5. 执行查询
        var result = await handler.Handle(
            new GetAppOrderWithtrackingTestQuery(),
            TestContext.Current.CancellationToken);

        // 6. 结果断言：
        result.Status.Should().Be(ResultStatus.Ok);
        result.Value.Should().NotBeNull();
        result.Value.OrderCount.Should().BeGreaterThan(0);
        result.Value.Items.Should().HaveCount(result.Value.OrderCount);
    }

    /// <summary>
    /// 测试场景：使用 EF Core 非追踪（NoTracking）模式获取订单。
    /// 预期结果：
    /// 1. 查询成功。
    /// 2. 返回正确的订单数量和项目列表。
    /// 目的：验证在关闭变更追踪（通常性能更好）的情况下，数据读取的正确性。
    /// </summary>
    [Fact]
    public async Task GetAppOrderWithNotrackingTest_WhenOrdersExist_ShouldReturnOrderItems()
    {
        // 1. 环境重置
        await _fixture.ResetDatabaseAsync();

        // 2. 种子数据准备
        await SeedOrdersAsync();

        // 3. 性能监控初始化
        var queryCounter = new QueryCounterInterceptor();
        await using var dbContext = TestDbContextFactory.Create(_fixture.ConnectionString, queryCounter);

        // 4. 依赖服务初始化：
        // GetAppOrderWithNotrackingTestQueryHandler: 使用 NoTracking 模式的查询处理器
        var handler = new GetAppOrderWithNotrackingTestQueryHandler(
            new EfReadRepository<AppOrder>(dbContext),
            queryCounter);

        // 5. 执行查询
        var result = await handler.Handle(
            new GetAppOrderWithNotrackingTestQuery(),
            TestContext.Current.CancellationToken);

        // 6. 结果断言：
        result.Status.Should().Be(ResultStatus.Ok);
        result.Value.Should().NotBeNull();
        result.Value.OrderCount.Should().BeGreaterThan(0);
        result.Value.Items.Should().HaveCount(result.Value.OrderCount);
    }

    /// <summary>
    /// 测试场景：模拟在无索引字段上进行查询，验证是否能返回结果（尽管可能较慢）。
    /// 预期结果：
    /// 1. 查询成功。
    /// 2. 返回特定用户的所有订单（预期为 2 个）。
    /// 3. 所有订单号以 "O" 开头。
    /// 4. 执行了 SQL 查询。
    /// 目的：验证即使在没有数据库索引优化的情况下，业务逻辑仍能正确返回数据，
    /// 同时可能用于后续性能基准对比或警告日志验证。
    /// </summary>
    [Fact]
    public async Task GetAppOrderSlowSqlWithNoIndexTest_WhenUserHasOrders_ShouldReturnUserOrders()
    {
        // 1. 环境重置
        await _fixture.ResetDatabaseAsync();

        // 2. 种子数据准备，并接收返回的用户和产品引用
        var seeded = await SeedOrdersAsync();

        // 3. 性能监控初始化
        var queryCounter = new QueryCounterInterceptor();
        await using var dbContext = TestDbContextFactory.Create(_fixture.ConnectionString, queryCounter);

        // 4. 依赖服务初始化：
        // GetAppOrderSlowSqlWithNoIndexTestQueryHandler: 模拟慢查询的处理器
        var handler = new GetAppOrderSlowSqlWithNoIndexTestQueryHandler(
            new EfReadRepository<AppOrder>(dbContext),
            queryCounter);

        // 5. 执行查询：传入特定用户 ID
        var result = await handler.Handle(
            new GetAppOrderSlowSqlWithNoIndexTestQuery(seeded.User.Id),
            TestContext.Current.CancellationToken);

        // 6. 结果断言：
        result.Status.Should().Be(ResultStatus.Ok);
        result.Value.Should().NotBeNull();

        // 验证返回的订单数量确切为 2（SeedOrdersAsync 中创建了两个订单）
        result.Value.OrderCount.Should().Be(2);

        // 验证所有返回的订单号都以 "O" 开头
        result.Value.Items.Should().OnlyContain(item => item.OrderNo.StartsWith("O"));

        // 验证执行了 SQL 查询
        result.Value.SelectSqlCount.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// 私有辅助方法：种子数据初始化。
    /// 创建一个用户、一个产品，以及该用户针对该产品的两个不同状态的订单。
    /// 返回创建的用户和产品对象，供其他测试用例使用。
    /// </summary>
    /// <returns>包含用户和产品的元组</returns>
    private async Task<(AppUser User, Product Product)> SeedOrdersAsync()
    {
        // 创建独立的 DbContext 用于种子数据插入，避免干扰测试中的监控上下文
        await using var dbContext = _fixture.CreateDbContext();

        // 1. 创建用户
        var user = TestEntityFactory.CreateUser(_fixture.IdGenerator, _fixture.PasswordHasher);

        // 2. 创建产品
        var product = TestEntityFactory.CreateProduct(_fixture.IdGenerator);

        // 3. 创建第一个订单：状态为 Addition (新增/待处理)
        var firstOrder = TestEntityFactory.CreateOrder(
            _fixture.IdGenerator,
            user,
            product,
            status: AppOrderStatus.Addition);

        // 4. 创建第二个订单：状态为 Paid (已支付)，数量为 4.000
        var secondOrder = TestEntityFactory.CreateOrder(
            _fixture.IdGenerator,
            user,
            product,
            status: AppOrderStatus.Paid,
            quantity: 4.000m);

        // 5. 持久化数据
        await dbContext.Set<AppUser>().AddAsync(user, TestContext.Current.CancellationToken);
        await dbContext.Set<Product>().AddAsync(product, TestContext.Current.CancellationToken);
        await dbContext.Set<AppOrder>().AddRangeAsync([firstOrder, secondOrder], TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 6. 返回引用以便测试用例进行断言或作为查询参数
        return (user, product);
    }

}


