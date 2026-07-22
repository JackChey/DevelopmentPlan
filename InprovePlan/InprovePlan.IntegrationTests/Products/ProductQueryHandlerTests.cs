using InprovePlan.Domain.Entities;
using InprovePlan.IntegrationTests.Helpers;
using InprovePlan.IntegrationTests.Infrastructure;
using InprovePlan.UserCase.Products.Queries;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.IntegrationTests.Products;

/// <summary>
/// 产品查询处理器集成测试类。
/// 此类专门用于测试与产品查询相关的业务逻辑（Query Handlers），包括根据 ID 获取单个产品和分页筛选获取产品列表。
/// 
/// 测试环境配置：
/// - 使用 [Collection(MySqlIntegrationTestCollection.Name)] 特性，表明该测试类属于 "mysql-integration-tests" 集合。
/// - 这意味着所有测试方法将共享同一个 MySqlTestFixture 实例，从而复用 MySQL Docker 容器和数据库连接，提高测试执行效率。
/// - 通过构造函数注入 MySqlTestFixture，以便在测试中访问数据库上下文工厂、ID 生成器等资源。
/// </summary>
[Collection(MySqlIntegrationTestCollection.Name)]
public sealed class ProductQueryHandlerTests
{
    /// <summary>
    /// MySQL 测试夹具实例。
    /// 由 xUnit 依赖注入框架自动提供，用于管理数据库生命周期、重置数据以及创建 DbContext。
    /// </summary>
    private readonly MySqlTestFixture _fixture;

    /// <summary>
    /// 构造函数。
    /// 接收 xUnit 注入的 MySqlTestFixture 实例，并将其保存为字段供测试方法使用。
    /// </summary>
    /// <param name="fixture">MySQL 测试夹具，提供数据库连接和工具方法。</param>
    public ProductQueryHandlerTests(MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// 测试根据 ID 获取产品查询处理器的逻辑：当产品存在时，应返回正确的产品信息。
    /// 
    /// 测试步骤：
    /// 1. 重置数据库，确保环境干净。
    /// 2. 创建新的 DbContext 实例。
    /// 3. 使用 TestEntityFactory 生成一个产品实体，并添加到数据库中保存。
    /// 4. 初始化 EfReadRepository（只读仓储）和 GetProductByIdQueryHandler。
    /// 5. 构建 GetProductByIdQuery，传入已保存产品的 ID。
    /// 6. 执行 handler.Handle 方法。
    /// 7. 验证返回结果的状态为 Ok，且返回值不为空。
    /// 8. 验证返回的产品 ID 和产品代码与数据库中保存的一致。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task GetProductById_WhenProductExists_ShouldReturnProduct()
    {
        // 重置数据库，清除之前测试留下的数据，保证测试隔离性
        await _fixture.ResetDatabaseAsync();

        // 创建一个新的 DbContext 实例，用于本次测试的数据操作
        await using var dbContext = _fixture.CreateDbContext();

        // 使用工厂方法创建一个产品实体，使用 Fixture 中的 ID 生成器确保 ID 唯一性
        var product = TestEntityFactory.CreateProduct(_fixture.IdGenerator);

        // 将产品添加到 DbContext 的变更追踪器中
        await dbContext.Set<Product>().AddAsync(product, TestContext.Current.CancellationToken);

        // 保存更改，将产品持久化到数据库
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 初始化只读产品仓储，通常用于查询场景，可能包含特定的查询优化或无追踪配置
        var repository = new EfReadRepository<Product>(dbContext);

        // 初始化根据 ID 获取产品的查询处理器
        var handler = new GetProductByIdQueryHandler(repository);

        // 执行查询处理逻辑，传入包含产品 ID 的查询对象
        var result = await handler.Handle(
            new GetProductByIdQuery(product.Id),
            TestContext.Current.CancellationToken);

        // 断言：处理结果状态应为成功 (Ok)
        result.Status.Should().Be(ResultStatus.Ok);

        // 断言：返回的值不应为空
        result.Value.Should().NotBeNull();

        // 断言：返回的产品 ID 应与请求的 ID 一致
        result.Value.Id.Should().Be(product.Id);

        // 断言：返回的产品代码应与数据库中保存的一致
        result.Value.ProductCode.Should().Be(product.ProductCode);
    }

    /// <summary>
    /// 测试分页获取产品列表查询处理器的逻辑：当存在符合过滤条件的产品时，应返回正确的分页结果。
    /// 
    /// 测试步骤：
    /// 1. 重置数据库。
    /// 2. 创建 DbContext 并生成两个产品实体：
    ///    - matched: 名称包含 "Keyboard"，类型 ID 为 3001，预期被筛选出来。
    ///    - other: 名称为 "Mouse"，类型 ID 为 3002，预期被过滤掉。
    /// 3. 将两个产品批量添加到数据库并保存。
    /// 4. 初始化 EfReadRepository 和 GetProductsPagedQueryHandler。
    /// 5. 构建 GetProductsPagedQuery，设置分页参数、排序参数以及过滤条件（关键字 "Key"，类型 ID 3001，状态 Enable）。
    /// 6. 执行 handler.Handle 方法。
    /// 7. 验证返回结果的状态为 Ok，且返回值不为空。
    /// 8. 验证返回的总记录数 Total 为 1。
    /// 9. 验证返回的项目列表 Items 中仅包含 matched 产品。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task GetProductsPaged_WhenProductsMatchFilter_ShouldReturnPagedProducts()
    {
        // 重置数据库，确保环境干净
        await _fixture.ResetDatabaseAsync();

        // 创建 DbContext 实例
        await using var dbContext = _fixture.CreateDbContext();

        // 创建一个符合筛选条件的产品：名称包含 "Key" (Keyboard)，类型 ID 为 3001
        var matched = TestEntityFactory.CreateProduct(
            _fixture.IdGenerator,
            name: "Keyboard",
            productTypeId: 3001);

        // 创建一个不符合筛选条件的产品：名称不包含 "Key"，类型 ID 不同
        var other = TestEntityFactory.CreateProduct(
            _fixture.IdGenerator,
            name: "Mouse",
            productTypeId: 3002);

        // 批量添加两个产品到 DbContext
        await dbContext.Set<Product>().AddRangeAsync([matched, other], TestContext.Current.CancellationToken);

        // 保存更改，将产品持久化到数据库
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 初始化只读产品仓储
        var repository = new EfReadRepository<Product>(dbContext);

        // 初始化分页获取产品列表的查询处理器
        var handler = new GetProductsPagedQueryHandler(repository);

        // 构建分页查询对象
        // - Page(): 使用默认分页参数（第1页，每页10条）
        // - Sort(): 使用默认排序参数（按 createdAt 降序）
        // - Keyword: "Key"，用于模糊匹配产品名称
        // - ProductTypeId: 3001，用于精确匹配产品类型
        // - ProductStatus: Enable，用于匹配产品状态
        var query = new GetProductsPagedQuery(
            TestQueryFactory.Page(),
            TestQueryFactory.Sort(),
            Keyword: "Key",
            ProductTypeId: 3001,
            ProductStatus: AppProductStatus.Enable);

        // 执行查询处理逻辑
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // 断言：处理结果状态应为成功 (Ok)
        result.Status.Should().Be(ResultStatus.Ok);

        // 断言：返回的值不应为空
        result.Value.Should().NotBeNull();

        // 断言：符合条件的总记录数应为 1（只有 matched 产品符合所有条件）
        result.Value.Total.Should().Be(1);

        // 断言：返回的项目列表中应仅包含 matched 产品
        // ContainSingle 确保列表中只有一个元素，且该元素的 ID 与 matched 一致
        result.Value.Items.Should().ContainSingle(item => item.Id == matched.Id);
    }
}

