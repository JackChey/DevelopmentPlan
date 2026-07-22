using InprovePlan.Domain.Entities;
using InprovePlan.IntegrationTests.Helpers;
using InprovePlan.IntegrationTests.Infrastructure;
using InprovePlan.UserCase.Products.Commands;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.IntegrationTests.Products;

/// <summary>
/// 产品命令处理器集成测试类。
/// 此类专门用于测试与产品创建、更新和删除相关的业务逻辑（Command Handlers）。
/// 
/// 测试环境配置：
/// - 使用 [Collection(MySqlIntegrationTestCollection.Name)] 特性，表明该测试类属于 "mysql-integration-tests" 集合。
/// - 这意味着所有测试方法将共享同一个 MySqlTestFixture 实例，从而复用 MySQL Docker 容器和数据库连接，提高测试执行效率。
/// - 通过构造函数注入 MySqlTestFixture，以便在测试中访问数据库上下文工厂、ID 生成器等资源。
/// </summary>
[Collection(MySqlIntegrationTestCollection.Name)]
public sealed class ProductCommandHandlerTests
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
    public ProductCommandHandlerTests(MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// 测试创建产品命令处理器的逻辑：当产品代码不存在时，应成功创建产品。
    /// 
    /// 测试步骤：
    /// 1. 重置数据库，确保环境干净。
    /// 2. 创建新的 DbContext 实例。
    /// 3. 初始化 EfRepository 和 CreateProductCommandHandler。
    /// 4. 构建 CreateProductCommand，包含带有空格的产品代码、名称、描述、类型ID、单价和货币。
    /// 5. 执行 handler.Handle 方法。
    /// 6. 验证返回结果的状态为 Ok，且返回值不为空。
    /// 7. 验证返回的产品对象属性是否正确处理（如去除空格、大写转换、价格四舍五入、默认状态等）。
    /// 8. 再次查询数据库，验证产品是否真正持久化存在。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task CreateProduct_WhenProductCodeDoesNotExist_ShouldCreateProduct()
    {
        // 重置数据库，清除之前测试留下的数据，保证测试隔离性
        await _fixture.ResetDatabaseAsync();

        // 创建一个新的 DbContext 实例，用于本次测试的数据操作
        await using var dbContext = _fixture.CreateDbContext();

        // 初始化产品仓储，使用 EF Core 实现
        var repository = new EfRepository<Product>(dbContext);

        // 初始化创建产品命令处理器
        var handler = new CreateProductCommandHandler(repository);

        // 构建创建产品命令
        // 注意：输入数据包含前后空格和小写货币，用于验证处理器的清洗和格式化逻辑
        var command = new CreateProductCommand(
            ProductCode: " p-new-001 ",
            ProductName: " New Product ",
            ProductDescription: " New Product Description ",
            ProductTypeId: 1001,
            UnitPrice: 12.346m,
            Currency: "rmb");

        // 执行命令处理逻辑
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // 断言：处理结果状态应为成功 (Ok)
        result.Status.Should().Be(ResultStatus.Ok);

        // 断言：返回的值不应为空
        result.Value.Should().NotBeNull();

        // 断言：产品代码应被修剪空格并转换为大写 ("P-NEW-001")
        result.Value.ProductCode.Should().Be("P-NEW-001");

        // 断言：产品名称应被修剪空格 ("New Product")
        result.Value.ProductName.Should().Be("New Product");

        // 断言：单价应四舍五入到两位小数 (12.35)
        result.Value.UnitPrice.Should().Be(12.35m);

        // 断言：产品状态应默认为启用 (Enable)
        result.Value.ProductStatus.Should().Be(AppProductStatus.Enable);

        // 验证数据持久化：查询数据库中是否存在该产品代码的记录
        var exists = await repository.AnyAsync(
            product => product.ProductCode == "P-NEW-001",
            TestContext.Current.CancellationToken);

        // 断言：数据库中应存在该记录
        exists.Should().BeTrue();
    }

    /// <summary>
    /// 测试更新产品命令处理器的逻辑：当产品存在时，应成功更新产品信息。
    /// 
    /// 测试步骤：
    /// 1. 重置数据库。
    /// 2. 创建 DbContext 并使用 TestEntityFactory 生成一个初始产品实体。
    /// 3. 将初始产品添加到数据库并保存。
    /// 4. 初始化 EfRepository 和 UpdateProductCommandHandler。
    /// 5. 构建 UpdateProductCommand，包含新的名称、描述、类型ID、状态、价格和货币。
    /// 6. 执行 handler.Handle 方法。
    /// 7. 验证返回结果的状态为 Ok，且返回值不为空。
    /// 8. 验证返回的产品对象属性已更新为预期值（如修剪空格、大写货币、价格四舍五入等）。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task UpdateProduct_WhenProductExists_ShouldUpdateProduct()
    {
        // 重置数据库，确保环境干净
        await _fixture.ResetDatabaseAsync();

        // 创建 DbContext 实例
        await using var dbContext = _fixture.CreateDbContext();

        // 使用工厂方法创建一个初始产品实体，使用 Fixture 中的 ID 生成器确保 ID 唯一性
        var product = TestEntityFactory.CreateProduct(_fixture.IdGenerator);

        // 将产品添加到 DbContext 的变更追踪器中
        await dbContext.Set<Product>().AddAsync(product, TestContext.Current.CancellationToken);

        // 保存更改，将产品持久化到数据库
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 初始化产品仓储
        var repository = new EfRepository<Product>(dbContext);

        // 初始化更新产品命令处理器
        var handler = new UpdateProductCommandHandler(repository);

        // 构建更新产品命令
        // 注意：输入数据包含前后空格和小写货币，用于验证处理器的清洗和格式化逻辑
        var command = new UpdateProductCommand(
            Id: product.Id,
            ProductName: " Updated Product ",
            ProductDescription: " Updated Product Description ",
            ProductTypeId: 2002,
            ProductStatus: AppProductStatus.SoldOut,
            UnitPrice: 88.888m,
            Currency: "usd");

        // 执行命令处理逻辑
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // 断言：处理结果状态应为成功 (Ok)
        result.Status.Should().Be(ResultStatus.Ok);

        // 断言：返回的值不应为空
        result.Value.Should().NotBeNull();

        // 断言：产品名称应被修剪空格 ("Updated Product")
        result.Value.ProductName.Should().Be("Updated Product");

        // 断言：产品描述应被修剪空格 ("Updated Product Description")
        result.Value.ProductDescription.Should().Be("Updated Product Description");

        // 断言：产品类型 ID 应更新为 2002
        result.Value.ProductTypeId.Should().Be(2002);

        // 断言：产品状态应更新为售罄 (SoldOut)
        result.Value.ProductStatus.Should().Be(AppProductStatus.SoldOut);

        // 断言：单价应四舍五入到两位小数 (88.89)
        result.Value.UnitPrice.Should().Be(88.89m);

        // 断言：货币代码应转换为大写 ("USD")
        result.Value.Currency.Should().Be("USD");
    }

    /// <summary>
    /// 测试删除产品命令处理器的逻辑：当产品存在时，应将产品标记为作废（Void），而非物理删除。
    /// 
    /// 测试步骤：
    /// 1. 重置数据库。
    /// 2. 创建 DbContext 并生成一个初始产品实体。
    /// 3. 将初始产品添加到数据库并保存。
    /// 4. 初始化 EfRepository 和 DeleteProductCommandHandler。
    /// 5. 构建 DeleteProductCommand，传入产品 ID。
    /// 6. 执行 handler.Handle 方法。
    /// 7. 验证返回结果的状态为 Ok。
    /// 8. 验证内存中的产品实体状态已变更为 Void（注意：这里直接检查了之前创建的实体对象，因为 EF Core 追踪了它）。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task DeleteProduct_WhenProductExists_ShouldMarkProductAsVoid()
    {
        // 重置数据库，确保环境干净
        await _fixture.ResetDatabaseAsync();

        // 创建 DbContext 实例
        await using var dbContext = _fixture.CreateDbContext();

        // 使用工厂方法创建一个初始产品实体
        var product = TestEntityFactory.CreateProduct(_fixture.IdGenerator);

        // 将产品添加到 DbContext 并保存
        await dbContext.Set<Product>().AddAsync(product, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 初始化产品仓储
        var repository = new EfRepository<Product>(dbContext);

        // 初始化删除产品命令处理器
        var handler = new DeleteProductCommandHandler(repository);

        // 执行删除命令处理逻辑
        var result = await handler.Handle(
            new DeleteProductCommand(product.Id),
            TestContext.Current.CancellationToken);

        // 断言：处理结果状态应为成功 (Ok)
        result.Status.Should().Be(ResultStatus.Ok);

        // 断言：产品实体的状态应被标记为作废 (Void)
        // 由于 product 对象仍被 DbContext 追踪，Handler 内部的修改会反映在该对象上
        product.ProductStatus.Should().Be(AppProductStatus.Void);
    }
}

