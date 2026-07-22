using DotNet.Testcontainers.Containers;
using InprovePlan.IntegrationTests.TestDoubles;
using Instructure.Caching;
using MySqlConnector;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace InprovePlan.IntegrationTests.Infrastructure;

/// <summary>
/// Redis 测试夹具类，用于在集成测试中管理 Redis 容器的生命周期、连接建立及数据清理。
/// 该类实现了 IAsyncLifetime 接口，确保在测试集合开始时初始化 Redis 环境，在结束时释放资源。
/// 主要功能包括启动 Docker 容器、建立 StackExchange.Redis 连接、提供缓存服务实例以及支持数据库重置。
/// </summary>
public class RedisTestFixture : IAsyncLifetime
{

    /// <summary>
    /// Redis Docker 容器实例。
    /// 使用 Testcontainers 库构建，配置为使用 redis:7.0 镜像。
    /// 该容器将在测试集合开始时启动，并在结束时销毁。
    /// </summary>
    private RedisContainer _container;

    /// <summary>
    /// Redis 连接多路复用器实例。
    /// 用于管理与 Redis 服务器的连接，支持异步操作和高并发访问。
    /// </summary>
    private IConnectionMultiplexer _connection = default!;

    /// <summary>
    /// 缓存配置选项。
    /// 定义了测试环境下的缓存行为，如应用名称、环境标识、键版本、默认过期时间等。
    /// 这些配置将用于初始化 CacheKeyBuilder 和 AppCache 实例。
    /// </summary>
    public readonly CacheOptions CacheOptions = new CacheOptions()
    {
        AppName = "InprovePlan-Test",
        Environment = "test",
        KeyVersion = "v1",
        DefaultDurationSeconds = 300,
        NullValueDurationSeconds = 60,
        JitterMaxSeconds = 30
    };

    /// <summary>
    /// 应用缓存服务实例。
    /// 基于 Redis 实现的缓存包装器，用于在测试中模拟真实的缓存读写操作。
    /// </summary>
    public IAppCache AppCache { get; private set; } = default!;

    /// <summary>
    /// 缓存键构建器实例。
    /// 用于生成标准化的缓存键，确保键的唯一性和规范性。
    /// 使用 FakeCacheKeyBuilder 以简化测试逻辑。
    /// </summary>
    public ICacheKeyBuilder CacheKeyBuilder { get; private set; } = default!;

    /// <summary>
    /// 构造函数。
    /// 初始化 Redis 容器构建器，指定使用 redis:7.0 镜像。
    /// 此时容器尚未启动，仅在 InitializeAsync 中启动。
    /// </summary>
    public RedisTestFixture()
    {
        _container = new RedisBuilder("redis:7.0")
                .Build();
    }

    /// <summary>
    /// 异步释放方法，由 xUnit 在测试集合结束后自动调用。
    /// 负责关闭 Redis 连接并销毁 Docker 容器，防止资源泄漏。
    /// 
    /// 释放顺序：
    /// 1. 如果连接存在，则异步关闭连接。
    /// 2. 如果容器存在，则异步 dispose 容器，停止并移除 Docker 容器。
    /// </summary>
    /// <returns>表示异步释放操作的任务。</returns>
    public async ValueTask DisposeAsync()
    {
        // 关闭 Redis 连接，释放网络资源
        if (_connection != null)
            await _connection.CloseAsync();

        // 销毁 Docker 容器
        // 容器停止后，其中存储的所有数据会自动消失，无需手动清除数据
        if (_container != null)
            await _container.DisposeAsync();
    }

    /// <summary>
    /// 异步初始化方法，由 xUnit 在测试集合开始前自动调用。
    /// 执行步骤如下：
    /// 1. 启动 Redis Docker 容器。
    /// 2. 验证容器状态是否为 Running，若失败则抛出异常。
    /// 3. 获取容器连接字符串，并构建 StackExchange.Redis 的配置选项（启用 Admin 权限、设置重试策略等）。
    /// 4. 建立 Redis 连接。
    /// 5. 初始化 CacheKeyBuilder 和 AppCache 实例。
    /// 6. 执行 Ping 命令验证连接有效性。
    /// 
    /// 若任何步骤失败，将捕获异常并打印详细日志以便排查 Docker 或网络问题。
    /// </summary>
    /// <returns>表示异步初始化操作的任务。</returns>
    public async ValueTask InitializeAsync()
    {
        try
        {
            // 1. 启动 Redis 容器，等待其就绪
            await _container.StartAsync();

            // 2. 验证容器是否成功启动并处于运行状态
            if (_container.State != TestcontainersStates.Running)
            {
                throw new InvalidOperationException("Redis container failed to start.");
            }

            // 3. 获取容器提供的连接字符串（通常为 host:port 格式）
            var connectionString = _container.GetConnectionString();

            // 构建 StackExchange.Redis 的连接配置选项
            // Testcontainers 返回的基础连接字符串可能需要补充额外参数以满足客户端需求
            var csBuilder = new ConfigurationOptions();
            csBuilder.EndPoints.Add(connectionString); // 添加终结点
            csBuilder.AllowAdmin = true;               // 允许执行管理命令（如 FLUSHDB），用于后续数据重置
            csBuilder.AbortOnConnectFail = false;      // 连接失败时不立即中止，允许重试
            csBuilder.ConnectRetry = 5;                // 连接重试次数
            csBuilder.SyncTimeout = 5000;              // 同步操作超时时间（毫秒）

            // 4. 异步建立 Redis 连接
            _connection = await ConnectionMultiplexer.ConnectAsync(csBuilder);

            // 初始化缓存键构建器，使用测试专用的 Fake 实现
            CacheKeyBuilder = new FakeCacheKeyBuilder(CacheOptions);

            // 初始化应用缓存服务，传入连接字符串和配置选项
            AppCache = new RedisAppCacheForTest(connectionString, CacheOptions);

            // 获取数据库实例并执行 Ping 命令，验证连接是否真正可用
            var db = _connection.GetDatabase();
            await db.PingAsync();
        }
        catch (Exception ex)
        {
            // 捕获初始化过程中的任何异常
            // 记录详细错误信息到控制台，方便开发者排查是 Docker 启动问题、网络问题还是配置问题
            Console.WriteLine($"Failed to initialize Redis fixture: {ex.Message}");
            // 重新抛出异常，导致测试集合初始化失败，避免后续测试在无效环境下运行
            throw;
        }
    }

    /// <summary>
    /// 重置 Redis 数据库数据方法。
    /// 建议在每个集成测试方法的开始前调用，以确保缓存环境的干净状态。
    /// 
    /// 执行逻辑：
    /// 1. 检查连接状态，若连接断开或未初始化，则尝试重新建立连接（确保包含 allowAdmin 权限）。
    /// 2. 获取 Redis 服务器实例。
    /// 3. 执行 FlushDatabaseAsync 清空当前数据库中的所有键值对。
    /// 
    /// 注意：
    /// 此操作会永久删除当前数据库中的所有数据，请确保仅在测试环境中使用。
    /// </summary>
    /// <returns>表示异步重置操作的任务。</returns>
    public async Task ResetDatabaseAsync()
    {
        // 检查连接是否有效
        if (_connection == null || !_connection.IsConnected)
        {
            // 如果连接断开，重新初始化连接
            // 确保连接字符串中包含 allowAdmin=true 和 abortConnect=false，以便执行管理命令和稳定连接
            var connectionString = _container.GetConnectionString();
            if (!connectionString.Contains("allowAdmin")) connectionString += ",allowAdmin=true";
            if (!connectionString.Contains("abortConnect")) connectionString += ",abortConnect=false";

            // 重新建立连接
            _connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
        }

        // 获取唯一的终结点（单机模式通常只有一个）
        var endpoint = _connection.GetEndPoints().Single();

        // 获取对应的服务器实例
        var server = _connection.GetServer(endpoint);

        // 异步清空当前数据库中的所有数据
        // 由于 AllowAdmin 已启用，此操作将被允许执行
        await server.FlushDatabaseAsync();
    }
}

