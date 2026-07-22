using InprovePlan.IntegrationTests.Builders;
using InprovePlan.IntegrationTests.DataSeeders;
using InprovePlan.IntegrationTests.Infrastructure;
using InprovePlan.IntegrationTests.TestData;
using InprovePlan.IntegrationTests.TestDoubles;
using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.AppOrders.Commands;
using Instructure.Repositories;

namespace InprovePlan.IntegrationTests.AppOrders;

/// <summary>
/// 应用订单命令处理器的集成测试类。
/// 使用 [Collection] 特性确保测试在指定的 MySQL 集成测试集合中串行执行，
/// 以避免数据库状态竞争或并行执行导致的数据冲突。
/// </summary>
[Collection(MySqlIntegrationTestCollection.Name)]
public class AppOrderCommandHandlerTests
{
    // 测试夹具（Fixture），用于管理测试生命周期内的共享资源，如数据库连接、ID生成器等
    private readonly MySqlTestFixture _fixture;

    /// <summary>
    /// 构造函数，通过依赖注入获取测试夹具实例。
    /// </summary>
    /// <param name="fixture">MySQL 测试环境配置实例</param>
    public AppOrderCommandHandlerTests(MySqlTestFixture fixture)
    {
        this._fixture = fixture;
    }

    /// <summary>
    /// 测试场景：当产品存在且当前用户存在时，执行创建订单命令。
    /// 预期结果：命令处理成功，返回的订单信息包含正确的产品代码、名称、单价、货币及初始状态。
    /// </summary>
    [Fact]
    public async Task Handle_WhenProductExistsAndCurrentUserExists_ShouldCreateOrder()
    {
        // 1. 环境准备：重置数据库，确保测试在一个干净的状态下开始
        await _fixture.ResetDatabaseAsync();

        // 2. 数据构建：使用 Builder 模式创建测试所需的用户和产品实体
        // AppUserBuilder: 构建一个应用用户，使用 Fixture 提供的 ID 生成器和密码哈希器
        var user = new AppUserBuilder(_fixture.IdGenerator, _fixture.PasswordHasher).Build();

        // ProductBuilder: 构建一个产品实体，使用 Fixture 提供的 ID 生成器
        var product = new ProductBuilder(_fixture.IdGenerator).Build();

        // 3. 模拟当前用户上下文
        // FakeCurrentUser: 模拟身份认证服务，设置当前操作用户的 ID 为上述创建的 user.Id
        var currentUser = new FakeCurrentUser() { Id = user.Id };

        // 4. 数据库上下文初始化
        // 创建一个新的 DbContext 实例，用于本次测试的数据种子化和命令执行
        await using var dbContext = _fixture.CreateDbContext();

        // 5. 数据种子化：将测试数据写入数据库
        var dataSeeder = new AppDbContextDataSeeder(dbContext);

        // 异步保存用户数据到数据库
        await dataSeeder.SeedAppUserAsync(user);

        // 异步保存产品数据到数据库
        await dataSeeder.SeedProductAsync(product);

        // 6. 构建命令对象
        // CreateAppOrderCommandBuilder: 使用链式调用构建创建订单的命令
        var command = new CreateAppOrderCommandBuilder()
            .WithProductId(product.Id)          // 设置要购买的产品 ID
            .WithQuantity(AppOrderTestData.ValidQuantity) // 设置有效的购买数量
            .WithAddressId(AppOrderTestData.ValidAddressId) // 设置有效的收货地址 ID
            .Build();                           // 生成最终的命令对象

        // 7. 初始化依赖服务
        // EfRepository<AppOrder>: 订单实体的读写仓库，用于持久化新订单
        var orderRepository = new EfRepository<AppOrder>(dbContext);

        // EfReadRepository<Product>: 产品实体的只读仓库，用于在创建订单前验证产品存在性及获取产品信息
        var productRepository = new EfReadRepository<Product>(dbContext);

        // 8. 实例化命令处理器
        // 注入订单仓库、产品仓库以及当前用户上下文
        var commandhandler = new CreateAppOrderCommandHandler(
                                orderRepository,
                                productRepository,
                                currentUser);

        // 9. 执行命令
        // 调用 Handle 方法处理创建订单请求，传入命令和取消令牌
        var result = await commandhandler.Handle(command, CancellationToken.None);

        // 10. 断言验证
        // 验证命令执行是否成功
        result.IsSuccess.Should().BeTrue();

        // 验证返回的订单值不为空，且产品代码与数据库中 seeded 的产品一致
        result.Value!.ProductCode.Should().Be(product.ProductCode);

        // 验证返回的产品名称与数据库中 seeded 的产品一致
        result.Value.ProductName.Should().Be(product.ProductName);

        // 验证返回的单价与数据库中 seeded 的产品单价一致（确保价格是从数据库读取而非前端传入，防止篡改）
        result.Value.UnitPrice.Should().Be(product.UnitPrice);

        // 验证货币类型与数据库中 seeded 的产品一致
        result.Value.Currency.Should().Be(product.Currency);

        // 验证订单初始状态是否为 "Addition"（新增/待处理状态）
        result.Value.OrderStatus.Should().Be(AppOrderStatus.Addition);
    }
}
