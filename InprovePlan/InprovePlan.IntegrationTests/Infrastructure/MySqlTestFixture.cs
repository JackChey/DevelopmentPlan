using InprovePlan.IntegrationTests.TestDoubles;
using Instructure.Data;
using Instructure.Interfaces;
using Instructure.IResult;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Respawn;
using Testcontainers.MySql;
using Xunit;

namespace InprovePlan.IntegrationTests.Infrastructure;

/// <summary>
/// MySQL 集成测试 Fixture。
///
/// 作用：
/// 1. 使用 Testcontainers 启动一个真实 MySQL 容器。
/// 2. 使用 EF Core 对测试数据库执行迁移。
/// 3. 使用 Respawn 在每个测试前清理数据库数据。
/// 4. 为集成测试提供 AppDbContext、测试 Id 生成器、测试密码哈希器。
///
/// 为什么不用 EF Core InMemory：
/// InMemory 不是关系型数据库，无法真实验证：
/// - MySQL 唯一索引
/// - 外键约束
/// - decimal 精度
/// - datetime 行为
/// - SQL 翻译
/// - LIMIT/OFFSET 分页
///
/// 因此仓储层、分页查询、排序、条件查询这类测试，
/// 更推荐使用真实 MySQL。
/// </summary>
public sealed class MySqlTestFixture : IAsyncLifetime
{
    /// <summary>
    /// MySQL 测试容器。
    ///
    /// 测试启动时自动拉起 MySQL 8.4。
    /// 测试结束后自动销毁容器。
    /// </summary>
    private readonly MySqlContainer _container = new MySqlBuilder()
        .WithImage("mysql:8.4")
        .WithDatabase("inproveplan_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    /// <summary>
    /// Respawn 数据库清理器。
    ///
    /// 用于在测试之间清空数据表，
    /// 避免测试数据互相污染。
    /// </summary>
    private Respawner _respawner = default!;

    /// <summary>
    /// 给 Respawn 使用的 MySQL 连接。
    ///
    /// Respawn 新版本接收 DbConnection，
    /// 不再直接接收 connection string。
    /// </summary>
    private MySqlConnection _connection = default!;

    /// <summary>
    /// 测试用 EF Core DbContext。
    ///
    /// 集成测试通过它直接插入、查询、断言数据库数据。
    /// </summary>
    public AppDbContext DbContext { get; private set; } = default!;

    /// <summary>
    /// 测试用 Id 生成器。
    ///
    /// 因为当前实体配置了 ValueGeneratedNever，
    /// 所以测试数据必须手动设置 Id。
    /// </summary>
    public IIdGenerator IdGenerator { get; } = new FakeIdGenerator();

    /// <summary>
    /// 测试用密码哈希器。
    ///
    /// 使用确定性 Hash，方便断言。
    /// 生产环境不要使用 FakePasswordHasher。
    /// </summary>
    public IPasswordHasher PasswordHasher { get; } = new FakePasswordHasher();

    /// <summary>
    /// 当前 MySQL 容器的连接字符串。
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// xUnit 在测试集合启动前调用。
    ///
    /// 执行步骤：
    /// 1. 启动 MySQL 容器。
    /// 2. 创建 EF Core DbContext。
    /// 3. 执行数据库迁移。
    /// 4. 打开 MySQL 连接。
    /// 5. 创建 Respawn 清理器。
    ///
    /// 当前 xUnit 版本要求返回 ValueTask。
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                ConnectionString,
                ServerVersion.AutoDetect(ConnectionString))
            .Options;

        DbContext = new AppDbContext(options);

        await DbContext.Database.MigrateAsync();

        _connection = new MySqlConnection(ConnectionString);

        await _connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.MySql
        });
    }

    /// <summary>
    /// 重置数据库数据。
    ///
    /// 建议每个集成测试开始前调用一次。
    ///
    /// 注意：
    /// 1. 先清理 ChangeTracker，避免 EF Core 仍跟踪旧实体。
    /// 2. 使用 Respawn 清空数据库表数据。
    /// 3. 再次清理 ChangeTracker，确保测试从干净状态开始。
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        DbContext.ChangeTracker.Clear();

        await _respawner.ResetAsync(_connection);

        DbContext.ChangeTracker.Clear();
    }

    /// <summary>
    /// xUnit 在测试集合结束后调用。
    ///
    /// 释放顺序：
    /// 1. 释放 DbContext。
    /// 2. 释放 MySQL 连接。
    /// 3. 销毁 MySQL 测试容器。
    ///
    /// 当前 xUnit 版本要求返回 ValueTask。
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();

        await _connection.DisposeAsync();

        await _container.DisposeAsync();
    }
}