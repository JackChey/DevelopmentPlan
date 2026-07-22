using Instructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

namespace InprovePlan.ApiTests.TestDoubles;

/// <summary>
/// 基于 Redis 和 FusionCache 的应用缓存实现，专用于测试环境。
/// 实现了 IAppCache 接口，提供分布式缓存功能，支持空值缓存、故障安全（Fail-Safe）以及通过 Redis Backplane 进行多节点缓存同步。
/// </summary>
internal class RedisAppCacheForTest : IAppCache
{
    /// <summary>
    /// FusionCache 实例，用于执行实际的缓存操作。
    /// 在构造函数中通过依赖注入容器初始化。
    /// </summary>
    private IFusionCache _cache = default!;

    /// <summary>
    /// Redis 连接字符串，用于配置分布式缓存和背板（Backplane）。
    /// </summary>
    private string _redisConnectionString;

    /// <summary>
    /// 缓存配置选项，包含默认 TTL、空值 TTL 等策略信息。
    /// </summary>
    private CacheOptions _options;

    /// <summary>
    /// 初始化 <see cref="RedisAppCacheForTest"/> 类的新实例。
    /// 配置并构建 FusionCache 服务提供者。
    /// </summary>
    /// <param name="redisConnectionString">Redis 服务器的连接字符串。</param>
    /// <param name="options">缓存策略配置选项。</param>
    public RedisAppCacheForTest(string redisConnectionString, CacheOptions options)
    {
        this._redisConnectionString = redisConnectionString;
        this._options = options;

        // 初始化 FusionCache 实例
        GetFusionCache();
    }

    /// <summary>
    /// 配置并创建 FusionCache 实例。
    /// 设置 Redis 分布式缓存、序列化器、默认条目选项（包括持续时间、故障安全机制、工厂超时）以及 Redis 背板以实现多节点同步。
    /// </summary>
    public void GetFusionCache()
    {
        var services = new ServiceCollection();

        // 配置 StackExchangeRedis 作为分布式缓存后端
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = _redisConnectionString;
        });

        // 配置 FusionCache
        services.AddFusionCache()
           // 设置默认条目选项
           .WithDefaultEntryOptions(new FusionCacheEntryOptions()
               .SetDuration(TimeSpan.FromMinutes(5)) // 默认缓存有效期 5 分钟
               .SetFailSafe(true, TimeSpan.FromMinutes(30)) // 启用故障安全模式，过期后保留 30 分钟
               .SetFactoryTimeouts(
                   TimeSpan.FromMilliseconds(300), // 软超时：300ms 后返回旧数据（如果有）
                   TimeSpan.FromSeconds(2))) // 硬超时：2s 后抛出异常
                                             // 使用 System.Text.Json 作为序列化器
           .WithSystemTextJsonSerializer()
           // 绑定分布式缓存服务
           .WithDistributedCache(provider =>
           {
               return provider.GetRequiredService<IDistributedCache>();
           })
           // 配置 Redis 背板，用于在多应用节点间同步缓存失效事件
           .WithBackplane(new RedisBackplane(new RedisBackplaneOptions
           {
               Configuration = _redisConnectionString,
           }));

        // 构建服务提供者并获取 IFusionCache 实例
        var serviceProvider = services.BuildServiceProvider();
        _cache = serviceProvider.GetRequiredService<IFusionCache>();
    }

    /// <summary>
    /// 获取或设置缓存项。
    /// 如果缓存中存在有效值，则直接返回；否则调用工厂方法获取数据并写入缓存。
    /// 支持空值缓存策略：如果工厂返回 null，根据配置决定是缓存空值还是移除键。
    /// </summary>
    /// <typeparam name="T">缓存值的类型，必须为引用类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <param name="factory">用于生成缓存值的异步工厂方法。</param>
    /// <param name="policy">缓存条目策略，包含持续时间、空值处理等配置。如果为 null，则使用默认策略。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>缓存中的值，如果不存在且工厂也返回 null，则返回 null。</returns>
    public async ValueTask<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CacheEntryPolicy? policy = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        // 如果未提供策略，则使用默认策略
        policy ??= CreateDefaultPolicy();

        var loadedByFactory = false;
        // 创建正常的 FusionCache 条目选项
        var normalOptions = CreateFusionCacheEntryOptions(policy.Duration);

        // 使用 CacheEnvelope 包装值，以区分“缓存中无值”和“缓存值为 null”
        var envelope = await _cache.GetOrSetAsync<CacheEnvelope<T>>(
        key,
        async ct =>
        {
            loadedByFactory = true;

            // 不捕获异常：
            // 有旧缓存时由 FusionCache Fail-Safe 处理；
            // 没有旧缓存时异常继续向上传递给全局异常处理器。
            var value = await factory(ct);

            // 将结果包装在信封中，即使是 null 也要包装，以便后续判断
            return value is null
                ? CacheEnvelope<T>.Null()
                : CacheEnvelope<T>.FromValue(value);
        },
        options: normalOptions,
        token: cancellationToken);

        // 如果值是由工厂加载的，并且结果为 null（即信封中没有值）
        if (loadedByFactory && !envelope.HasValue)
        {
            if (policy.CacheNullValue)
            {
                // 如果允许缓存空值，使用独立的空值 TTL 覆盖首次写入的正常 TTL。
                // 禁用故障安全，因为空值通常不需要 fail-safe
                var nullValueOptions =
                    CreateFusionCacheEntryOptions(policy.NullValueDuration, enableFailSafe: false);

                await _cache.SetAsync(
                    key,
                    envelope,
                    options: nullValueOptions,
                    token: cancellationToken);
            }
            else
            {
                // 如果禁止缓存空值，清除 GetOrSetAsync 临时写入的空包装，避免占用缓存空间
                await _cache.RemoveAsync(
                    key,
                    options: null,
                    token: cancellationToken);
            }
        }

        // 明确创建 FusionCacheEntryOptions，避免因为方法重载不同导致编译错误。
        // 注意：此处变量 entryOptions 声明但未在后续逻辑中直接使用，可能是遗留代码或用于调试/扩展
        var entryOptions = CreateFusionCacheEntryOptions(policy.Duration);

        // 对调用方来说，缓存空值时仍然表现为 null。
        // 如果信封中有值，返回该值；否则返回 null
        return envelope.HasValue
        ? envelope.Value
        : null;
    }

    /// <summary>
    /// 设置缓存项。
    /// 将值包装在 CacheEnvelope 中并存入缓存。
    /// </summary>
    /// <typeparam name="T">缓存值的类型，必须为引用类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <param name="value">要缓存的值。</param>
    /// <param name="policy">缓存条目策略。如果为 null，则使用默认策略。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步设置操作的任务。</returns>
    public ValueTask SetAsync<T>(
        string key,
        T value,
        CacheEntryPolicy? policy = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        policy ??= CreateDefaultPolicy();

        var entryOptions = CreateFusionCacheEntryOptions(policy.Duration);

        // 注意这里使用具名参数 options 和 token。
        // 如果直接把 cancellationToken 放在第 4 个参数位置，
        // 可能会被编译器误认为是 tags 参数，导致重载匹配失败。
        return _cache.SetAsync(
            key,
            CacheEnvelope<T>.FromValue(value),
            options: entryOptions,
            token: cancellationToken);
    }

    /// <summary>
    /// 移除缓存项。
    /// 从缓存中删除指定键的数据。如果配置了 Redis Backplane，它会通知其他应用节点清理本地 L1 缓存。
    /// </summary>
    /// <param name="key">要移除的缓存键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步移除操作的任务。</returns>
    public ValueTask RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        // FusionCache 的 RemoveAsync 第二个参数是 FusionCacheEntryOptions?，
        // 第三个参数才是 CancellationToken。
        //
        // 所以不要写：
        // _cache.RemoveAsync(key, cancellationToken)
        //
        // 应该写成下面这样，明确传递 token。
        //
        // 使用 FusionCache 删除的好处是：
        // 如果配置了 Redis Backplane，它会通知其他应用节点清理本地 L1 缓存。
        return _cache.RemoveAsync(
            key,
            options: null,
            token: cancellationToken);
    }

    /// <summary>
    /// 获取缓存项。
    /// 尝试从缓存中获取指定键的值。如果不存在或已过期，则返回 null。
    /// 支持通过 CacheEnvelope 区分“键不存在”和“值为 null”的情况。
    /// </summary>
    /// <typeparam name="T">缓存值的类型，必须为引用类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>缓存中的值，如果不存在则返回 null。</returns>
    public async ValueTask<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
        where T : class
    {
        // 尝试异步获取缓存信封，使用 TryGetAsync 避免在键不存在时抛出异常或产生不必要的开销
        var maybeEnvelope = await _cache.TryGetAsync<CacheEnvelope<T>>(
            key,
            token: cancellationToken);

        // 如果缓存中完全没有该键（TryGet 返回默认值或 HasValue 为 false），直接返回 null
        if (!maybeEnvelope.HasValue)
        {
            return null;
        }

        var envelope = maybeEnvelope.Value;

        // 检查信封内部是否包含有效值。
        // 如果信封存在但内部值为 null（即之前缓存了空值），则返回 null。
        // 如果信封内部有值，则返回该值。
        return envelope.HasValue
            ? envelope.Value
            : null;
    }

    /// <summary>
    /// 创建 FusionCache 条目选项配置。
    /// 配置缓存的有效期、故障安全（Fail-Safe）机制、超时策略以及分布式缓存的超时设置，
    /// 以平衡数据一致性、系统可用性和后端负载。
    /// </summary>
    /// <param name="duration">业务逻辑上的缓存有效期。经过此时间后，数据被视为过期，需要重新从源加载。</param>
    /// <param name="enableFailSafe">是否启用故障安全机制。启用后，在后端异常时可返回过期的旧数据以提升可用性。</param>
    /// <returns>配置好的 FusionCacheEntryOptions 实例。</returns>
    private FusionCacheEntryOptions CreateFusionCacheEntryOptions(TimeSpan duration, bool enableFailSafe = true)
    {
        return new FusionCacheEntryOptions
        {
            // 业务缓存的逻辑有效期。
            // 过了这个时间，FusionCache 会认为数据过期，需要重新回源。
            // 添加随机抖动以避免缓存雪崩（大量 Key 同时过期）。
            Duration = AddJitter(duration),

            // 开启 Fail-Safe。
            // 当数据库、Redis 或 factory 短暂异常时，如果缓存中有旧值，
            // FusionCache 可以返回旧值，提升接口可用性。
            IsFailSafeEnabled = enableFailSafe,

            // 旧值最多允许被 Fail-Safe 使用多久。
            // 例如正常 TTL 是 5 分钟，这里是 30 分钟：
            // 5 分钟后数据逻辑过期，但异常时最多 30 分钟内还能兜底返回旧值。
            FailSafeMaxDuration = enableFailSafe
                ? TimeSpan.FromMinutes(30)
                : TimeSpan.Zero,

            // Fail-Safe 触发后，短时间内不要一直反复回源。
            // 避免数据库或 Redis 故障时，大量请求持续打到后端。
            FailSafeThrottleDuration = enableFailSafe
                ? TimeSpan.FromSeconds(30)
                : TimeSpan.Zero,

            // FactorySoftTimeout：
            // 如果回源超过 300ms，且存在旧缓存，FusionCache 可以先返回旧值。
            FactorySoftTimeout = TimeSpan.FromMilliseconds(300),

            // FactoryHardTimeout：
            // 回源最大允许 2 秒，超过后强制超时。
            FactoryHardTimeout = TimeSpan.FromSeconds(2),

            // Redis 分布式缓存软超时。
            // 当 Redis 响应慢且本地有旧值时，可以快速返回旧值。
            DistributedCacheSoftTimeout = TimeSpan.FromMilliseconds(200),

            // Redis 分布式缓存硬超时。
            // 避免 Redis 慢请求拖垮业务接口。
            DistributedCacheHardTimeout = TimeSpan.FromSeconds(1),
        };
    }

    /// <summary>
    /// 创建默认的缓存条目策略。
    /// 从配置选项中读取默认的持续时间、空值持续时间以及是否缓存空值。
    /// </summary>
    /// <returns>默认的 CacheEntryPolicy 实例。</returns>
    private CacheEntryPolicy CreateDefaultPolicy()
    {
        return new CacheEntryPolicy
        {
            // 默认缓存有效期，从配置中读取秒数
            Duration = TimeSpan.FromSeconds(_options.DefaultDurationSeconds),

            // 空值缓存的有效期，从配置中读取秒数
            NullValueDuration = TimeSpan.FromSeconds(_options.NullValueDurationSeconds),

            // 是否允许缓存 null 值
            CacheNullValue = true
        };
    }

    /// <summary>
    /// 为给定的持续时间添加随机抖动（Jitter）。
    /// 旨在避免大量缓存键在同一时刻过期，从而防止缓存雪崩效应，减轻后端存储压力。
    /// </summary>
    /// <param name="duration">基础缓存持续时间。</param>
    /// <returns>添加了随机抖动后的新持续时间。</returns>
    private TimeSpan AddJitter(TimeSpan duration)
    {
        // 如果配置的最大抖动秒数小于等于 0，则不添加抖动，直接返回原始持续时间
        if (_options.JitterMaxSeconds <= 0)
        {
            return duration;
        }

        // 给 TTL 加随机抖动，避免大量 Key 在同一时间过期。
        // 例如基础 TTL 是 5 分钟，JitterMaxSeconds = 30，
        // 最终 TTL 会在 5:00 到 5:30 之间随机分布。
        return duration.Add(TimeSpan.FromSeconds(
            Random.Shared.Next(0, _options.JitterMaxSeconds)));
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        return _cache.ClearAsync(token: cancellationToken);
    }
}

