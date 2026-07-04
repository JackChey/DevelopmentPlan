namespace Instructure.Repositories;

using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Redis 仓储实现类。
/// 
/// 该类实现了 IRedisRepository 接口，提供基于 StackExchange.Redis 客户端的异步数据访问能力。
/// 所有复杂对象均通过 System.Text.Json 进行序列化和反序列化，以 JSON 字符串形式存储在 Redis 中。
/// </summary>
public sealed class RedisRepository : IRedisRepository
{
    // Redis 数据库实例，用于执行具体的命令操作
    private readonly IDatabase _database;

    // JSON 序列化配置选项
    // - PropertyNamingPolicy.CamelCase: 将 C# 的 PascalCase 属性名转换为 JSON 的 camelCase 格式（例如 UserName -> userName）
    // - WriteIndented: false: 生成紧凑的 JSON 字符串，不包含换行和缩进，以节省存储空间和网络带宽
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// 构造函数，初始化 Redis 仓储。
    /// </summary>
    /// <param name="redis">Redis 连接多路复用器，由依赖注入容器提供</param>
    public RedisRepository(IConnectionMultiplexer redis)
    {
        // 从连接多路复用器中获取默认的数据库实例（通常为 DB 0）
        _database = redis.GetDatabase();
    }

    /// <summary>
    /// 异步获取指定键的值，并反序列化为指定类型。
    /// </summary>
    /// <typeparam name="T">期望返回的数据类型</typeparam>
    /// <param name="key">Redis 键名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>
    /// 如果键存在，返回反序列化后的对象；
    /// 如果键不存在或值为空，返回 default(T)（通常为 null）。
    /// </returns>
    public async Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        // 从 Redis 获取字符串值
        var value = await _database.StringGetAsync(key);

        // 检查返回值是否为 null 或空字符串
        if (value.IsNullOrEmpty)
        {
            return default;
        }

        // 将 JSON 字符串反序列化为目标类型 T
        // value! 使用 null-forgiving 运算符，因为前面已检查过 IsNullOrEmpty
        return JsonSerializer.Deserialize<T>(value!, JsonOptions);
    }

    /// <summary>
    /// 异步设置指定键的值，将对象序列化为 JSON 存储。
    /// </summary>
    /// <typeparam name="T">要存储的对象类型</typeparam>
    /// <param name="key">Redis 键名</param>
    /// <param name="value">要存储的对象实例</param>
    /// <param name="expiry">可选的过期时间。如果为 null，则键永不过期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果设置成功，返回 true；否则返回 false</returns>
    public async Task<bool> SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        // 将对象序列化为 JSON 字符串
        var json = JsonSerializer.Serialize(value, JsonOptions);

        // 执行 SET 命令
        // StackExchange.Redis 的 StringSetAsync 会自动处理 null 值和过期时间
        return await _database.StringSetAsync(
        key,
        json,
        expiry,
        When.Always);
    }

    /// <summary>
    /// 异步删除指定键。
    /// </summary>
    /// <param name="key">要删除的 Redis 键名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果键被成功删除，返回 true；如果键不存在，返回 false</returns>
    public async Task<bool> RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        // 执行 DEL 命令
        return await _database.KeyDeleteAsync(key);
    }

    /// <summary>
    /// 异步检查指定键是否存在。
    /// </summary>
    /// <param name="key">要检查的 Redis 键名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果键存在，返回 true；否则返回 false</returns>
    public async Task<bool> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        // 执行 EXISTS 命令
        return await _database.KeyExistsAsync(key);
    }

    /// <summary>
    /// 异步对指定键的值进行原子递增操作。
    /// 注意：此方法假设键对应的值是整数或可转换为整数的字符串。
    /// </summary>
    /// <param name="key">要递增的 Redis 键名</param>
    /// <param name="value">递增步长，默认为 1。可为负数以实现递减</param>
    /// <param name="expiry">可选的过期时间。仅在键是新创建时或显式设置时生效</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>递增操作完成后的新值</returns>
    public async Task<long> IncrementAsync(
        string key,
        long value = 1,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        // 执行 INCRBY 命令，原子性地增加键的值
        // 如果键不存在，Redis 会先将其初始化为 0，然后再执行递增
        var result = await _database.StringIncrementAsync(key, value);

        // 如果提供了过期时间，则单独设置键的过期时间
        // 注意：这里分为两步执行（递增 + 设置过期），非原子操作。
        // 在极高并发且键刚创建的场景下，可能存在极小的竞态窗口，但通常对于业务缓存场景是可接受的。
        // 若需严格原子性，建议使用 Lua 脚本组合 INCR 和 EXPIRE。
        if (expiry.HasValue)
        {
            await _database.KeyExpireAsync(key, expiry);
        }

        return result;
    }

    /// <summary>
    /// 异步设置哈希表（Hash）中指定字段的值。
    /// 对应 Redis 命令 HSET。
    /// </summary>
    /// <typeparam name="T">字段值的数据类型</typeparam>
    /// <param name="key">哈希表所在的键名</param>
    /// <param name="field">哈希表中的字段名</param>
    /// <param name="value">要设置的字段值，将被序列化为 JSON</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>
    /// 如果字段是新建的，返回 true；
    /// 如果字段已存在并被更新，返回 false。
    /// （注：StackExchange.Redis 的 HashSetAsync 返回 bool，true 表示新建字段，false 表示更新字段）
    /// </returns>
    public async Task<bool> SetHashAsync<T>(
        string key,
        string field,
        T value,
        CancellationToken cancellationToken = default)
    {
        // 将字段值序列化为 JSON 字符串
        var json = JsonSerializer.Serialize(value, JsonOptions);

        // 执行 HSET 命令
        return await _database.HashSetAsync(key, field, json);
    }

    /// <summary>
    /// 异步获取哈希表（Hash）中指定字段的值。
    /// 对应 Redis 命令 HGET。
    /// </summary>
    /// <typeparam name="T">期望返回的字段值类型</typeparam>
    /// <param name="key">哈希表所在的键名</param>
    /// <param name="field">要获取值的字段名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>
    /// 如果字段存在，返回反序列化后的对象；
    /// 如果字段不存在，返回 default(T)。
    /// </returns>
    public async Task<T?> GetHashAsync<T>(
        string key,
        string field,
        CancellationToken cancellationToken = default)
    {
        // 执行 HGET 命令
        var value = await _database.HashGetAsync(key, field);

        // 检查返回值是否为 null 或空
        if (value.IsNullOrEmpty)
        {
            return default;
        }

        // 将 JSON 字符串反序列化为目标类型 T
        return JsonSerializer.Deserialize<T>(value!, JsonOptions);
    }
}
