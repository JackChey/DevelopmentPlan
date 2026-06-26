namespace InprovePlan.UserCase.Caching;

using Instructure.Caching;
using Instructure.Exceptions;
using Instructure.SystemLogs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using ZiggyCreatures.Caching.Fusion;

public sealed class AppCache(IFusionCache _cache,IOptions<CacheOptions> configureOptions) : IAppCache
{
    private readonly CacheOptions _options = configureOptions.Value;

    public async ValueTask<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CacheEntryPolicy? policy = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        policy ??= CreateDefaultPolicy();

        var loadedByFactory = false;
        var normalOptions = CreateFusionCacheEntryOptions(policy.Duration);

        var envelope = await _cache.GetOrSetAsync<CacheEnvelope<T>>(
        key,
        async ct =>
        {
            loadedByFactory = true;

            // 不捕获异常：
            // 有旧缓存时由 FusionCache Fail-Safe 处理；
            // 没有旧缓存时异常继续向上传递给全局异常处理器。
            var value = await factory(ct);

            return value is null
                ? CacheEnvelope<T>.Null()
                : CacheEnvelope<T>.FromValue(value);
        },
        options: normalOptions,
        token: cancellationToken);

        if (loadedByFactory && !envelope.HasValue)
        {
            if (policy.CacheNullValue)
            {
                // 使用独立的空值 TTL 覆盖首次写入的正常 TTL。
                var nullValueOptions =
                    CreateFusionCacheEntryOptions(policy.NullValueDuration);

                await _cache.SetAsync(
                    key,
                    envelope,
                    options: nullValueOptions,
                    token: cancellationToken);
            }
            else
            {
                // 禁止缓存空值时，清除 GetOrSetAsync 临时写入的空包装。
                await _cache.RemoveAsync(
                    key,
                    options: null,
                    token: cancellationToken);
            }
        }

        // 明确创建 FusionCacheEntryOptions，避免因为方法重载不同导致编译错误。
        var entryOptions = CreateFusionCacheEntryOptions(policy.Duration);

        // 对调用方来说，缓存空值时仍然表现为 null。
        return envelope.HasValue
        ? envelope.Value
        : null;
    }

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

    private FusionCacheEntryOptions CreateFusionCacheEntryOptions(TimeSpan duration)
    {
        return new FusionCacheEntryOptions
        {
            // 业务缓存的逻辑有效期。
            // 过了这个时间，FusionCache 会认为数据过期，需要重新回源。
            Duration = AddJitter(duration),

            // 开启 Fail-Safe。
            // 当数据库、Redis 或 factory 短暂异常时，如果缓存中有旧值，
            // FusionCache 可以返回旧值，提升接口可用性。
            IsFailSafeEnabled = true,

            // 旧值最多允许被 Fail-Safe 使用多久。
            // 例如正常 TTL 是 5 分钟，这里是 30 分钟：
            // 5 分钟后数据逻辑过期，但异常时最多 30 分钟内还能兜底返回旧值。
            FailSafeMaxDuration = TimeSpan.FromMinutes(30),

            // Fail-Safe 触发后，短时间内不要一直反复回源。
            // 避免数据库或 Redis 故障时，大量请求持续打到后端。
            FailSafeThrottleDuration = TimeSpan.FromSeconds(30),

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

    private CacheEntryPolicy CreateDefaultPolicy()
    {
        return new CacheEntryPolicy
        {
            Duration = TimeSpan.FromSeconds(_options.DefaultDurationSeconds),
            NullValueDuration = TimeSpan.FromSeconds(_options.NullValueDurationSeconds),
            CacheNullValue = true
        };
    }

    private TimeSpan AddJitter(TimeSpan duration)
    {
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
}
