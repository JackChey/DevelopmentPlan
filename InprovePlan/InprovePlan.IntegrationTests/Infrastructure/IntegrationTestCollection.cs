namespace InprovePlan.IntegrationTests.Infrastructure;

/// <summary>
/// 集成测试集合定义类，用于将多个测试类分组并共享固定的测试资源。
/// 此类定义了名为 "integration-tests" 的测试集合，该集合中的测试类将共享 MySqlTestFixture 和 RedisTestFixture 实例。
/// 适用于需要同时依赖 MySQL 数据库和 Redis 缓存的完整集成测试场景。
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection
    : ICollectionFixture<MySqlTestFixture>, ICollectionFixture<RedisTestFixture>
{
    /// <summary>
    /// 测试集合的唯一名称标识。
    /// 在测试类上使用 [Collection(Name)] 特性时，需引用此常量以加入该集合。
    /// </summary>
    public const string Name = "integration-tests";
}

/// <summary>
/// MySQL 集成测试集合定义类，用于将仅依赖 MySQL 数据库的测试类分组。
/// 此类定义了名为 "mysql-integration-tests" 的测试集合，该集合中的测试类将共享 MySqlTestFixture 实例。
/// 适用于只需要数据库环境而不需要 Redis 环境的轻量级集成测试场景，有助于减少资源开销。
/// </summary>
[CollectionDefinition(Name)]
public sealed class MySqlIntegrationTestCollection
    : ICollectionFixture<MySqlTestFixture>
{
    /// <summary>
    /// 测试集合的唯一名称标识。
    /// 在测试类上使用 [Collection(Name)] 特性时，需引用此常量以加入该集合。
    /// </summary>
    public const string Name = "mysql-integration-tests";
}

/// <summary>
/// Redis 集成测试集合定义类，用于将仅依赖 Redis 缓存的测试类分组。
/// 此类定义了名为 "redis-integration-tests" 的测试集合，该集合中的测试类将共享 RedisTestFixture 实例。
/// 适用于只需要缓存环境而不需要数据库环境的特定集成测试场景，有助于隔离测试依赖。
/// </summary>
[CollectionDefinition(Name)]
public sealed class RedisIntegrationTestCollection
    : ICollectionFixture<RedisTestFixture>
{
    /// <summary>
    /// 测试集合的唯一名称标识。
    /// 在测试类上使用 [Collection(Name)] 特性时，需引用此常量以加入该集合。
    /// </summary>
    public const string Name = "redis-integration-tests";
}

