using InprovePlan.IntegrationTests.TestDoubles;
using Instructure.Data;
using Instructure.Interfaces;
using Instructure.IResult;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Respawn;
using Testcontainers.MySql;

namespace InprovePlan.IntegrationTests.Infrastructure;

/// <summary>
/// MySQL 测试夹具类，用于在集成测试中管理 MySQL 容器的生命周期、数据库迁移及数据清理。
/// 该类实现了 IAsyncLifetime 接口，确保在测试集合开始时初始化资源，在结束时释放资源。
/// 主要功能包括启动 Docker 容器、执行 EF Core 迁移、提供干净的数据库连接以及支持 Respawn 数据重置。
/// </summary>
public class MySqlTestFixture : IAsyncLifetime
{

    /// <summary>
    /// MySQL Docker 容器实例。
    /// 使用 Testcontainers 库构建，配置为使用 mysql:8.4 镜像，并预设数据库名、用户名和密码。
    /// 该容器将在测试集合开始时启动，并在结束时销毁。
    /// </summary>
    private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.4")
                                                    .WithDatabase("inproveplan_test")
                                                    .WithUsername("test")
                                                    .WithPassword("test")
                                                    .Build();

    /// <summary>
    /// ID 生成器实例。
    /// 使用 FakeIdGenerator 模拟 ID 生成逻辑，避免依赖外部真实服务，确保测试的可重复性。
    /// </summary>
    public IIdGenerator IdGenerator { get; } = new FakeIdGenerator();

    /// <summary>
    /// 密码哈希器实例。
    /// 使用 FakePasswordHasher 模拟密码哈希逻辑，简化测试中的密码处理流程，提高测试执行速度。
    /// </summary>
    public IPasswordHasher PasswordHasher { get; } = new FakePasswordHasher();

    /// <summary>
    /// 获取当前 MySQL 容器的连接字符串。
    /// 该属性动态从运行中的容器获取连接信息，确保连接地址和端口的正确性。
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// 用于 Respawn 数据库清理工具的 MySQL 连接对象。
    /// Respawn 新版本要求直接传入 DbConnection 对象而非连接字符串，因此在此维护一个长连接实例。
    /// </summary>
    private MySqlConnection _connection = default!;

    /// <summary>
    /// Respawn 数据库清理器实例。
    /// 用于在测试之间快速清空数据表内容，保留表结构，从而避免测试数据互相污染，保证测试隔离性。
    /// </summary>
    private Respawner _respawner = default!;

    /// <summary>
    /// 异步初始化方法，由 xUnit 在测试集合开始前自动调用。
    /// 执行步骤如下：
    /// 1. 启动 MySQL Docker 容器。
    /// 2. 创建 DbContext 并执行数据库迁移（MigrateAsync），确保表结构最新。
    /// 3. 建立 MySqlConnection 连接并打开。
    /// 4. 初始化 Respawn 清理器，配置为 MySQL 适配器，以便后续进行数据重置。
    /// </summary>
    /// <returns>表示异步初始化操作的任务。</returns>
    public async ValueTask InitializeAsync()
    {
        // 启动 MySQL 容器，等待其就绪
        await _container.StartAsync();

        // 创建 DbContext 实例并应用所有待处理的迁移，确保数据库结构与代码模型一致
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();

        // 基于容器提供的连接字符串创建 MySQL 连接对象
        _connection = new MySqlConnection(ConnectionString);

        // 打开数据库连接，供 Respawn 使用
        await _connection.OpenAsync();

        // 创建 Respawn 实例，指定数据库适配器为 MySQL
        // Respawn 会扫描数据库结构，为后续的快速重置做准备
        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.MySql
        });
    }

    /// <summary>
    /// 重置数据库数据方法。
    /// 建议在每个集成测试方法的开始前调用，以确保测试环境的干净状态。
    /// 
    /// 注意：
    /// 1. 此方法仅通过 Respawn 清空表数据，不涉及表结构的变更。
    /// 2. 调用方需确保在执行此方法前，EF Core 的 ChangeTracker 已清理或不再跟踪旧实体，以避免状态冲突。
    /// 3. 若测试中使用了 DbContext，建议在调用此方法后重新创建 DbContext 实例或清理其追踪状态。
    /// </summary>
    /// <returns>表示异步重置操作的任务。</returns>
    public async Task ResetDatabaseAsync()
    {
        // 使用 Respawn 异步重置数据库，清空所有受管表的数据
        await _respawner.ResetAsync(_connection);
    }

    /// <summary>
    /// 创建新的 AppDbContext 实例。
    /// 使用当前的连接字符串初始化上下文，确保每次获取的都是指向同一测试数据库的新上下文实例。
    /// </summary>
    /// <returns>返回配置好的 AppDbContext 实例。</returns>
    public AppDbContext CreateDbContext()
    {
        return TestDbContextFactory.Create(ConnectionString);
    }

    /// <summary>
    /// 异步释放方法，由 xUnit 在测试集合结束后自动调用。
    /// 负责清理和释放所有占用的资源，防止资源泄漏。
    /// 
    /// 释放顺序：
    /// 1. 关闭并释放 MySQL 连接 (_connection)。
    /// 2. 停止并销毁 MySQL Docker 容器 (_container)。
    /// 
    /// 当前 xUnit 版本要求该方法返回 ValueTask 以优化性能。
    /// </summary>
    /// <returns>表示异步释放操作的任务。</returns>
    public async ValueTask DisposeAsync()
    {
        // 释放数据库连接资源
        await _connection.DisposeAsync();

        // 停止并移除 Docker 容器，释放系统资源
        await _container.DisposeAsync();
    }
}


