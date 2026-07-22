using InprovePlan.Domain.Entities;
using InprovePlan.IntegrationTests.Helpers;
using InprovePlan.IntegrationTests.Infrastructure;
using InprovePlan.IntegrationTests.TestDoubles;
using InprovePlan.UserCase.AppOrders;
using InprovePlan.UserCase.AppOrders.Commands;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.IntegrationTests.AppOrders;

/// <summary>
/// 应用程序订单附加命令处理器的集成测试类。
/// 该类主要测试带有幂等性控制的订单创建、订单更新（含缓存失效）以及订单删除（含缓存清理）的业务逻辑。
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class AppOrderAdditionalCommandHandlerTests
{
    // MySQL 测试夹具，用于管理关系型数据库的连接、上下文创建及数据重置
    private readonly MySqlTestFixture _mysqlFixture;

    // Redis 测试夹具，用于管理缓存服务的连接、数据重置及缓存操作验证
    private readonly RedisTestFixture _redisFixture;

    /// <summary>
    /// 初始化测试类的新实例。
    /// </summary>
    /// <param name="mysqlFixture">MySQL 测试夹具，由测试框架注入。</param>
    /// <param name="redisFixture">Redis 测试夹具，由测试框架注入。</param>
    public AppOrderAdditionalCommandHandlerTests(
        MySqlTestFixture mysqlFixture,
        RedisTestFixture redisFixture)
    {
        _mysqlFixture = mysqlFixture;
        _redisFixture = redisFixture;
    }

    /// <summary>
    /// 测试当产品和用户存在时，使用幂等性键创建订单应成功。
    /// 验证点：
    /// 1. 操作返回成功状态。
    /// 2. 订单关联的产品 ID 和用户 ID 正确。
    /// 3. 数量经过四舍五入处理（保留三位小数）。
    /// 4. 订单初始状态为“新增”。
    /// </summary>
    [Fact]
    public async Task CreateAppOrderWithIdempotency_WhenProductExistsAndCurrentUserExists_ShouldCreateOrder()
    {
        // 重置 MySQL 数据库状态，确保数据环境干净
        await _mysqlFixture.ResetDatabaseAsync();

        // 创建一个新的数据库上下文实例
        await using var dbContext = _mysqlFixture.CreateDbContext();

        // 使用工厂方法创建测试所需的用户实体
        var user = TestEntityFactory.CreateUser(_mysqlFixture.IdGenerator, _mysqlFixture.PasswordHasher);

        // 使用工厂方法创建测试所需的产品实体
        var product = TestEntityFactory.CreateProduct(_mysqlFixture.IdGenerator);

        // 将用户和产品直接通过 DbContext 添加到数据库并保存
        // 这里直接使用 DbSet 添加，而非使用 DataSeeder，展示了另一种数据准备方式
        await dbContext.Set<AppUser>().AddAsync(user, TestContext.Current.CancellationToken);
        await dbContext.Set<Product>().AddAsync(product, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 初始化订单写入仓库
        var orderRepository = new EfRepository<AppOrder>(dbContext);

        // 初始化产品只读仓库
        var productRepository = new EfReadRepository<Product>(dbContext);

        // 模拟当前登录用户
        var currentUser = new FakeCurrentUser { Id = user.Id };

        // 实例化带有幂等性控制的创建订单命令处理器
        var handler = new CreateAppOrderWithIdempotencyCommandHandler(
            orderRepository,
            productRepository,
            currentUser);

        // 构建创建订单命令，包含一个唯一的幂等性键（IdempotencyKey）
        // 数量为 3.4567，预期在业务逻辑中被格式化或舍入
        var command = new CreateAppOrderWithIdempotencyCommand(
            product.Id,
            Quantity: 3.4567m,
            AddressId: 90001,
            IdempotencyKey: Guid.NewGuid().ToString("N"));

        // 执行命令处理逻辑
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // --- 断言部分 ---

        // 验证处理结果状态为成功 (Ok)
        result.Status.Should().Be(ResultStatus.Ok);

        // 验证返回的订单对象不为空
        result.Value.Should().NotBeNull();

        // 验证订单中的产品 ID 与命令中指定的一致
        result.Value.ProductId.Should().Be(product.Id);

        // 验证订单中的用户 ID 与当前用户一致
        result.Value.UserId.Should().Be(user.Id);

        // 验证数量是否按预期进行了舍入（3.4567 -> 3.457）
        result.Value.Quantity.Should().Be(3.457m);

        // 验证订单初始状态为“新增”
        result.Value.OrderStatus.Should().Be(AppOrderStatus.Addition);
    }

    /// <summary>
    /// 测试当当前用户拥有处于“新增”状态的订单时，更新订单应成功并移除相关缓存。
    /// 验证点：
    /// 1. 更新操作返回成功，且字段（数量、地址ID、总金额）计算正确。
    /// 2. 更新后，Redis 中对应的订单缓存被清除（Cache Invalidation）。
    /// </summary>
    [Fact]
    public async Task UpdateAppOrder_WhenCurrentUserOwnsAdditionOrder_ShouldUpdateOrderAndRemoveCache()
    {
        // 重置 MySQL 和 Redis 状态
        await _mysqlFixture.ResetDatabaseAsync();
        await _redisFixture.ResetDatabaseAsync();

        await using var dbContext = _mysqlFixture.CreateDbContext();

        // 种子数据：创建一个处于“新增”状态的订单及其关联的用户和产品
        var (user, product, order) = await SeedOrderAsync(dbContext, AppOrderStatus.Addition);

        // 构建该订单在 Redis 中的缓存键
        var cacheKey = _redisFixture.CacheKeyBuilder.Build("order", "detail", order.Id);

        // 预置缓存：手动将该订单的 DTO 放入 Redis，模拟查询后产生的缓存
        await _redisFixture.AppCache.SetAsync(cacheKey, ToDto(order), cancellationToken: TestContext.Current.CancellationToken);

        // 初始化订单仓库
        var repository = new EfRepository<AppOrder>(dbContext);

        // 实例化更新订单命令处理器，注入缓存服务以支持缓存失效逻辑
        var handler = new UpdateAppOrderCommandHandler(
            repository,
            _redisFixture.AppCache,
            _redisFixture.CacheKeyBuilder,
            new FakeCurrentUser { Id = user.Id });

        // 执行更新命令，修改数量和地址 ID
        var result = await handler.Handle(
            new UpdateAppOrderCommand(order.Id, Quantity: 5.1234m, AddressId: 90909),
            TestContext.Current.CancellationToken);

        // --- 断言部分：验证业务逻辑 ---

        // 验证操作成功
        result.Status.Should().Be(ResultStatus.Ok);
        result.Value.Should().NotBeNull();

        // 验证数量是否按预期舍入（5.1234 -> 5.123）
        result.Value.Quantity.Should().Be(5.123m);

        // 验证地址 ID 已更新
        result.Value.AddressId.Should().Be(90909);

        // 验证总金额是否根据新数量和产品单价正确重新计算
        result.Value.TotalAmount.Should().Be(product.UnitPrice * 5.123m);

        // --- 断言部分：验证缓存失效 ---

        // 尝试从 Redis 获取之前的缓存数据
        var cached = await _redisFixture.AppCache.GetAsync<AppOrderDto>(
            cacheKey,
            TestContext.Current.CancellationToken);

        // 验证缓存已被清除，确保下次查询时会重新从数据库加载最新数据
        cached.Should().BeNull();
    }

    /// <summary>
    /// 测试当当前用户拥有处于“新增”状态的订单时，删除订单应成功并移除相关缓存。
    /// 验证点：
    /// 1. 删除操作返回成功状态。
    /// 2. 数据库中该订单记录已被物理删除或标记为不可见（AnyAsync 返回 false）。
    /// 3. Redis 中对应的订单详情缓存已被清除，保证数据一致性。
    /// </summary>
    [Fact]
    public async Task DeleteAppOrder_WhenCurrentUserOwnsAdditionOrder_ShouldRemoveOrderAndCache()
    {
        // 重置 MySQL 数据库和 Redis 缓存，确保测试环境隔离且干净
        await _mysqlFixture.ResetDatabaseAsync();
        await _redisFixture.ResetDatabaseAsync();

        //创建数据库上下文实例
        await using var dbContext = _mysqlFixture.CreateDbContext();

        // 种子数据：创建一个属于特定用户且状态为“新增”的订单
        // 忽略返回的产品信息，仅关注用户和订单
        var (user, _, order) = await SeedOrderAsync(dbContext, AppOrderStatus.Addition);

        // 构建该订单在 Redis 中的缓存键
        var cacheKey = _redisFixture.CacheKeyBuilder.Build("order", "detail", order.Id);

        // 预置缓存：手动将订单 DTO 写入 Redis，模拟该订单之前被查询过并产生了缓存
        await _redisFixture.AppCache.SetAsync(cacheKey, ToDto(order), cancellationToken: TestContext.Current.CancellationToken);

        // 初始化订单仓库，用于执行删除操作和后续的存在性检查
        var repository = new EfRepository<AppOrder>(dbContext);

        // 实例化删除订单命令处理器
        // 注入缓存服务和键构建器以支持缓存清理逻辑
        // 注入当前用户以进行权限校验（确保只能删除自己的订单）
        var handler = new DeleteAppOrderCommandHandler(
            repository,
            _redisFixture.AppCache,
            _redisFixture.CacheKeyBuilder,
            new FakeCurrentUser { Id = user.Id });

        // 执行删除命令
        var result = await handler.Handle(
            new DeleteAppOrderCommand(order.Id),
            TestContext.Current.CancellationToken);

        // --- 断言部分 ---

        // 验证操作执行成功
        result.Status.Should().Be(ResultStatus.Ok);

        // 验证数据库中是否还存在该 ID 的订单记录
        // 预期结果应为 False，表示订单已成功从数据库中移除
        var exists = await repository.AnyAsync(
            item => item.Id == order.Id,
            TestContext.Current.CancellationToken);

        exists.Should().BeFalse();

        // 验证 Redis 中该订单的缓存是否已被清除
        // 预期结果应为 Null，表示缓存失效逻辑已正确执行
        var cached = await _redisFixture.AppCache.GetAsync<AppOrderDto>(
            cacheKey,
            TestContext.Current.CancellationToken);

        cached.Should().BeNull();
    }

    /// <summary>
    /// 测试当订单存在时，更改订单状态应成功更新状态并移除相关缓存。
    /// 验证点：
    /// 1. 状态变更操作返回成功。
    /// 2. 订单状态已更新为目标状态（例如：Paid）。
    /// 3. Redis 中对应的订单详情缓存已被清除，防止读取到旧状态的缓存数据。
    /// </summary>
    [Fact]
    public async Task ChangeAppOrderStatus_WhenOrderExists_ShouldUpdateStatusAndRemoveCache()
    {
        // 重置 MySQL 数据库和 Redis 缓存
        await _mysqlFixture.ResetDatabaseAsync();
        await _redisFixture.ResetDatabaseAsync();

        await using var dbContext = _mysqlFixture.CreateDbContext();

        // 种子数据：创建一个状态为“新增”的订单
        var (_, _, order) = await SeedOrderAsync(dbContext, AppOrderStatus.Addition);

        // 构建缓存键
        var cacheKey = _redisFixture.CacheKeyBuilder.Build("order", "detail", order.Id);

        // 预置缓存：模拟订单详情已被缓存
        await _redisFixture.AppCache.SetAsync(cacheKey, ToDto(order), cancellationToken: TestContext.Current.CancellationToken);

        // 初始化订单仓库
        var repository = new EfRepository<AppOrder>(dbContext);

        // 实例化更改订单状态命令处理器
        // 注意：此处理器不依赖当前用户上下文，可能用于后台任务或系统级状态变更
        var handler = new ChangeAppOrderStatusCommandHandler(
            _redisFixture.AppCache,
            _redisFixture.CacheKeyBuilder,
            repository);

        // 执行状态变更命令，将订单状态改为“已支付”（Paid）
        var result = await handler.Handle(
            new ChangeAppOrderStatusCommand(order.Id, AppOrderStatus.Paid),
            TestContext.Current.CancellationToken);

        // --- 断言部分 ---

        // 验证操作成功
        result.Status.Should().Be(ResultStatus.Ok);

        // 验证返回的订单对象不为空
        result.Value.Should().NotBeNull();

        // 验证订单状态已正确更新为“已支付”
        result.Value.OrderStatus.Should().Be(AppOrderStatus.Paid);

        // 验证 Redis 缓存已被清除
        // 确保后续查询不会获取到状态为“新增”的旧缓存数据
        var cached = await _redisFixture.AppCache.GetAsync<AppOrderDto>(
            cacheKey,
            TestContext.Current.CancellationToken);

        cached.Should().BeNull();
    }

    /// <summary>
    /// 测试当订单存在时，使用带有幂等性和消息队列集成的命令处理器更改订单状态。
    /// 验证点：
    /// 1. 状态变更成功，且返回正确的订单状态。
    /// 2. 领域事件已发布到消息队列（通过 FakePublisher 验证）。
    /// 3. 事件中包含正确的订单 ID、变更原因和操作人 ID。
    /// 4. Redis 缓存已被清除。
    /// </summary>
    [Fact]
    public async Task ChangeAppOrderStatusWithIdempotencyAndMq_WhenOrderExists_ShouldPublishEventAndUpdateStatus()
    {
        // 重置 MySQL 数据库和 Redis 缓存
        await _mysqlFixture.ResetDatabaseAsync();
        await _redisFixture.ResetDatabaseAsync();

        await using var dbContext = _mysqlFixture.CreateDbContext();

        // 种子数据：创建一个属于特定用户且状态为“新增”的订单
        var (user, _, order) = await SeedOrderAsync(dbContext, AppOrderStatus.Addition);

        // 构建缓存键并预置缓存
        var cacheKey = _redisFixture.CacheKeyBuilder.Build("order", "detail", order.Id);
        await _redisFixture.AppCache.SetAsync(cacheKey, ToDto(order), cancellationToken: TestContext.Current.CancellationToken);

        // 初始化订单仓库
        var repository = new EfRepository<AppOrder>(dbContext);

        // 创建伪造的事件发布器，用于捕获和验证发布的事件
        var publisher = new FakeOrderEventPublisher();

        // 实例化带有幂等性和 MQ 集成的高级状态变更命令处理器
        var handler = new ChangeAppOrderStatusWithIdempotencyAndMqCommandHandler(
            _redisFixture.AppCache,
            _redisFixture.CacheKeyBuilder,
            new FakeCurrentUser { Id = user.Id }, // 提供操作人上下文
            publisher,                             // 注入事件发布器
            repository);

        // 执行状态变更命令
        // 包含幂等性键、变更原因等信息
        var result = await handler.Handle(
            new ChangeAppOrderStatusWithIdempotencyAndMqCommand(
                order.Id,
                AppOrderStatus.Paid,
                UpdateReason: "paid by integration test",
                IdempotencyKey: Guid.NewGuid().ToString("N")),
            TestContext.Current.CancellationToken);

        // --- 断言部分：验证业务结果 ---

        // 验证操作成功
        result.Status.Should().Be(ResultStatus.Ok);
        result.Value.Should().NotBeNull();

        // 验证订单状态已更新
        result.Value.OrderStatus.Should().Be(AppOrderStatus.Paid);

        // --- 断言部分：验证事件发布 ---

        // 验证恰好发布了一个事件
        publisher.Events.Should().ContainSingle();

        // 验证事件中的订单 ID 正确
        publisher.Events[0].OrderId.Should().Be(order.Id);

        // 验证事件中的变更原因与命令中指定的一致
        publisher.Events[0].Reason.Should().Be("paid by integration test");

        // 验证事件中的操作人 ID 与当前用户一致
        publisher.Events[0].OperatorId.Should().Be(user.Id);

        // --- 断言部分：验证缓存失效 ---

        // 验证 Redis 缓存已被清除
        var cached = await _redisFixture.AppCache.GetAsync<AppOrderDto>(
            cacheKey,
            TestContext.Current.CancellationToken);

        cached.Should().BeNull();
    }

    /// <summary>
    /// 异步种子化订单数据，创建并保存用户、产品及订单实体到数据库。
    /// </summary>
    /// <param name="dbContext">应用程序的数据库上下文，用于执行数据库操作。</param>
    /// <param name="status">订单的初始状态。</param>
    /// <returns>
    /// 一个包含已创建的用户 (<see cref="AppUser"/>)、产品 (<see cref="Product"/>) 和订单 (<see cref="AppOrder"/>) 的元组。
    /// </returns>
    private async Task<(AppUser User, Product Product, AppOrder Order)> SeedOrderAsync(
            Instructure.Data.AppDbContext dbContext,
            AppOrderStatus status)
    {
        // 使用测试实体工厂创建一个新的用户实例，依赖ID生成器和密码哈希器
        var user = TestEntityFactory.CreateUser(_mysqlFixture.IdGenerator, _mysqlFixture.PasswordHasher);

        // 使用测试实体工厂创建一个新的产品实例，依赖ID生成器
        var product = TestEntityFactory.CreateProduct(_mysqlFixture.IdGenerator);

        // 使用测试实体工厂创建一个新的订单实例，关联上述用户和产品，并设置指定状态
        var order = TestEntityFactory.CreateOrder(
            _mysqlFixture.IdGenerator,
            user,
            product,
            status: status);

        // 将用户实体异步添加到数据库上下文中，并传递取消令牌
        await dbContext.Set<AppUser>().AddAsync(user, TestContext.Current.CancellationToken);

        // 将产品实体异步添加到数据库上下文中，并传递取消令牌
        await dbContext.Set<Product>().AddAsync(product, TestContext.Current.CancellationToken);

        // 将订单实体异步添加到数据库上下文中，并传递取消令牌
        await dbContext.Set<AppOrder>().AddAsync(order, TestContext.Current.CancellationToken);

        // 异步保存所有更改到数据库，确保事务一致性，并传递取消令牌
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 返回创建的三个实体对象，供后续测试或验证使用
        return (user, product, order);
    }

    /// <summary>
    /// 将领域实体 <see cref="AppOrder"/> 转换为数据传输对象 (DTO) <see cref="AppOrderDto"/>。
    /// </summary>
    /// <param name="order">需要转换的订单领域实体。</param>
    /// <returns>包含订单详细信息的 DTO 对象。</returns>
    private static AppOrderDto ToDto(AppOrder order)
    {
        // 构造并返回 AppOrderDto 实例
        // 参数依次映射：ID, 订单号, 产品ID, 产品名称, 产品代码, 货币类型, 
        // 单价, 数量, 总价 (单价 * 数量), 用户ID, 发生时间, 订单状态, 
        // 是否取消, 地址ID
        return new AppOrderDto(
            order.Id,
            order.OrderNo,
            order.ProductId,
            order.ProductName,
            order.ProductCode,
            order.Currency,
            order.UnitPrice,
            order.Quantity,
            order.UnitPrice * order.Quantity, // 计算总价
            order.UserId,
            order.OccurredTime,
            order.OrderStatus,
            order.Cancelled,
            order.AddressId);
    }

}

