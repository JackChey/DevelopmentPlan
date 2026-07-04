namespace InprovePlan.UserCase.Idempotency;

using Instructure.Idempotency;
using StackExchange.Redis;

/// <summary>
/// Redis 分布式锁实现。
/// 
/// 核心原理：
/// 1. 加锁：利用 Redis 的 SET 命令配合 NX (Not Exists) 和 EX (Expire) 参数，确保只有当键不存在时才能设置成功，并自动设置过期时间以防止死锁。
/// 2. 解锁：使用 Lua 脚本保证“检查锁归属”和“删除锁”这两个操作的原子性，防止误删其他客户端持有的锁。
/// </summary>
public sealed class RedisDistributedLock(IConnectionMultiplexer _redis) : IDistributedLock
{
    /// <summary>
    /// 尝试异步获取分布式锁。
    /// </summary>
    /// <param name="key">锁的唯一标识键</param>
    /// <param name="expiration">锁的自动过期时间，防止持有锁的客户端崩溃导致死锁</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>
    /// 如果成功获取锁，返回一个实现了 IAsyncDisposable 的句柄，用于在 using 语句块结束时释放锁；
    /// 如果获取失败（锁已被其他客户端持有），返回 null。
    /// </returns>
    public async Task<IAsyncDisposable?> TryAcquireAsync(
        string key,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        // 获取 Redis 数据库实例
        var database = _redis.GetDatabase();

        // 生成唯一的锁令牌（Token）。
        // 使用 GUID 确保每个请求的锁拥有者标识唯一，这是安全解锁的关键。
        // "N" 格式表示不带连字符的 32 位十六进制字符串，节省存储空间。
        var token = Guid.NewGuid().ToString("N");

        // 执行原子性的加锁操作：
        // - key: 锁的键
        // - token: 锁的值（所有者标识）
        // - expiration: 过期时间
        // - When.NotExists (NX): 仅当键不存在时才设置值。如果键已存在，说明锁已被占用，返回 false。
        // 这一步保证了加锁的互斥性。
        var acquired = await database.StringSetAsync(
            key,
            token,
            expiration,
            When.NotExists);

        // 如果加锁失败，直接返回 null，调用方需处理重试或降级逻辑
        if (!acquired)
        {
            return null;
        }

        // 加锁成功，返回一个锁句柄。
        // 该句柄封装了释放锁所需的上下文（数据库、键、令牌），并在 DisposeAsync 中执行安全的解锁逻辑。
        return new RedisLockHandle(database, key, token);
    }

    /// <summary>
    /// 内部类：代表一个已获取的分布式锁句柄。
    /// 实现 IAsyncDisposable 以便支持 using await 语法，确保锁在使用完毕后被正确释放。
    /// </summary>
    private sealed class RedisLockHandle(
        IDatabase _database,
        string _key,
        string _token
        ) : IAsyncDisposable
    {

        /// <summary>
        /// 异步释放锁。
        /// 
        /// 重要：不能简单地调用 DEL 命令删除键，因为可能存在以下竞态条件：
        /// 1. 客户端 A 的锁过期了。
        /// 2. 客户端 B 成功获取了同一个 key 的锁。
        /// 3. 客户端 A 此时才执行 DEL 命令，结果误删了客户端 B 的锁。
        /// 
        /// 解决方案：使用 Lua 脚本保证“判断令牌是否匹配”和“删除键”这两个操作的原子性。
        /// 只有当当前存储的 Token 与加锁时生成的 Token 一致时，才执行删除操作。
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            // Lua 脚本：
            // KEYS[1]: 锁的键
            // ARGV[1]: 锁的令牌（Token）
            // 逻辑：如果键对应的值等于传入的令牌，则删除该键并返回 1；否则返回 0。
            const string script = """
                                  if redis.call("GET", KEYS[1]) == ARGV[1] then
                                      return redis.call("DEL", KEYS[1])
                                  else
                                      return 0
                                  end
                                  """;

            // 执行 Lua 脚本。
            // ScriptEvaluateAsync 确保脚本在 Redis 服务器端原子执行，中间不会被其他命令插入。
            await _database.ScriptEvaluateAsync(
                script,
                new RedisKey[] { _key },
                new RedisValue[] { _token });
        }
    }
}

