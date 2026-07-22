using InprovePlan.Domain.Entities;
using InprovePlan.IntegrationTests.Helpers;
using InprovePlan.IntegrationTests.Infrastructure;
using InprovePlan.UserCase.AppUsers.Queries;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.IntegrationTests.AppUsers;

/// <summary>
/// 应用用户查询处理器的集成测试类。
/// 该类专门用于测试涉及用户读取操作的查询处理器，如获取详情和分页列表。
/// 使用 [Collection] 特性确保测试在指定的 MySQL 集成测试集合中串行执行，
/// 以避免数据库状态竞争或并行执行导致的数据冲突。
/// </summary>
[Collection(MySqlIntegrationTestCollection.Name)]
public sealed class AppUserQueryHandlerTests
{
    // 测试夹具（Fixture），用于管理测试生命周期内的共享资源，如数据库连接、ID生成器等
    private readonly MySqlTestFixture _fixture;

    /// <summary>
    /// 构造函数，通过依赖注入获取测试夹具实例。
    /// </summary>
    /// <param name="fixture">MySQL 测试环境配置实例</param>
    public AppUserQueryHandlerTests(MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// 测试场景：当用户存在时，执行根据 ID 获取用户详情的查询。
    /// 预期结果：
    /// 1. 查询成功返回用户信息。
    /// 2. 返回的用户 ID 和用户名与数据库中存储的一致。
    /// </summary>
    [Fact]
    public async Task GetAppUserById_WhenUserExists_ShouldReturnUser()
    {
        // 1. 环境准备：重置数据库，确保测试在一个干净的状态下开始
        await _fixture.ResetDatabaseAsync();

        // 2. 数据库上下文初始化：创建一个新的 DbContext 实例用于本次测试
        await using var dbContext = _fixture.CreateDbContext();

        // 3. 种子数据准备：创建一个测试用户实体
        var user = TestEntityFactory.CreateUser(_fixture.IdGenerator, _fixture.PasswordHasher);

        // 将用户添加到上下文并保存到数据库
        await dbContext.Set<AppUser>().AddAsync(user, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 4. 依赖服务初始化：
        // EfReadRepository<AppUser>: 用户实体的只读仓库，用于执行查询操作
        var repository = new EfReadRepository<AppUser>(dbContext);

        // GetAppUserByIdQueryHandler: 根据 ID 获取用户详情的查询处理器
        var handler = new GetAppUserByIdQueryHandler(repository);

        // 5. 执行查询：传入要查询的用户 ID
        var result = await handler.Handle(
            new GetAppUserByIdQuery(user.Id),
            TestContext.Current.CancellationToken);

        // 6. 结果断言：
        // 验证查询执行状态为成功 (Ok)
        result.Status.Should().Be(ResultStatus.Ok);

        // 验证返回的用户对象不为空
        result.Value.Should().NotBeNull();

        // 验证返回的用户 ID 与请求的 ID 一致
        result.Value.Id.Should().Be(user.Id);

        // 验证返回的用户名与数据库中存储的用户名一致
        result.Value.UserName.Should().Be(user.UserName);
    }

    /// <summary>
    /// 测试场景：当存在符合过滤条件的用户时，执行分页获取用户列表查询。
    /// 预期结果：
    /// 1. 查询成功返回分页结果。
    /// 2. 总记录数（Total）正确反映了符合过滤条件的用户数量。
    /// 3. 返回的项目列表（Items）仅包含符合所有过滤条件（关键字、状态、性别）的用户。
    /// </summary>
    [Fact]
    public async Task GetAppUsersPaged_WhenUsersMatchFilter_ShouldReturnPagedUsers()
    {
        // 1. 环境准备：重置数据库
        await _fixture.ResetDatabaseAsync();

        // 2. 数据库上下文初始化
        await using var dbContext = _fixture.CreateDbContext();

        // 3. 种子数据准备：创建两个具有不同属性的用户
        // matched: 符合后续查询过滤条件的用户（用户名含"paged"，状态启用，性别女）
        var matched = TestEntityFactory.CreateUser(
            _fixture.IdGenerator,
            _fixture.PasswordHasher,
            userName: "paged-user",
            status: AppUserStatus.Enable,
            sex: AppUserSex.Female);

        // other: 不符合过滤条件的用户（用户名不含"paged"，状态冻结，性别男），用于验证过滤逻辑的有效性
        var other = TestEntityFactory.CreateUser(
            _fixture.IdGenerator,
            _fixture.PasswordHasher,
            userName: "other-user",
            status: AppUserStatus.Frozen,
            sex: AppUserSex.Male);

        // 将两个用户批量添加到上下文并保存到数据库
        await dbContext.Set<AppUser>().AddRangeAsync([matched, other], TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 4. 依赖服务初始化：
        // EfReadRepository<AppUser>: 用户只读仓库
        var repository = new EfReadRepository<AppUser>(dbContext);

        // GetAppUsersPagedQueryHandler: 分页获取用户列表的查询处理器
        var handler = new GetAppUsersPagedQueryHandler(repository);

        // 5. 构建分页查询对象：
        // TestQueryFactory.Page(): 提供默认的分页参数（如页码、每页大小）
        // TestQueryFactory.Sort(): 提供默认的排序参数
        // Keyword: "paged" -> 过滤用户名包含 "paged" 的用户
        // Status: AppUserStatus.Enable -> 过滤状态为启用的用户
        // Sex: AppUserSex.Female -> 过滤性别为女性的用户
        var query = new GetAppUsersPagedQuery(
            TestQueryFactory.Page(),
            TestQueryFactory.Sort(),
            Keyword: "paged",
            Status: AppUserStatus.Enable,
            Sex: AppUserSex.Female);

        // 6. 执行查询
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // 7. 结果断言：
        // 验证查询执行状态为成功 (Ok)
        result.Status.Should().Be(ResultStatus.Ok);

        // 验证返回的分页结果对象不为空
        result.Value.Should().NotBeNull();

        // 验证总记录数为 1，因为只有 'matched' 用户符合所有过滤条件，'other' 用户被排除
        result.Value.Total.Should().Be(1);

        // 验证返回的项目列表中仅包含 'matched' 用户
        // ContainSingle 确保列表中只有一个元素，且该元素的 ID 等于 matched.Id
        result.Value.Items.Should().ContainSingle(item => item.Id == matched.Id);
    }
}

