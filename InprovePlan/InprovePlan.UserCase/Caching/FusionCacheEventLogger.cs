namespace InprovePlan.UserCase.Caching;

using Instructure.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Events;

/// <summary>
/// FusionCache 高层事件日志记录器。
/// 
/// 该类实现 IHostedService 接口，作为后台服务随应用程序启动和停止。
/// 主要职责是订阅 FusionCache 的核心生命周期事件，并将关键操作以结构化日志的形式输出。
/// 
/// 设计原则与注意事项：
/// 1. 关注高层语义：仅订阅 Hit, Miss, Set, Remove, FailSafeActivate 这五个核心事件。
///    这些事件代表了缓存对外的最终状态，适合用于业务监控、告警和统计分析。
/// 
/// 2. 避免底层噪音：不建议直接订阅 Memory.Miss 或 Distributed.Miss 等底层事件并统一标记为 "cache.miss"。
///    底层事件在一次逻辑请求中可能触发多次（例如 L1 未命中但 L2 命中），这会导致统计数据失真，且更适合调试而非生产监控。
/// 
/// 3. 事件语义明确：
///    - Hit/Miss 代表最终结果。
///    - Set 代表缓存写入动作完成，而非数据加载完成。
///    - FailSafeActivate 是系统健康的重要指标，表明后端数据源出现异常。
/// </summary>
public sealed class FusionCacheEventLogger : IHostedService
{
    // FusionCache 实例引用。
    // 用于访问其 Events 属性以订阅/取消订阅缓存生命周期事件。
    private readonly IFusionCache _cache;

    // 日志记录器实例。
    // 用于将缓存事件输出到配置的日志提供者（如 Console, File, Seq, ELK 等）。
    private readonly ILogger<FusionCacheEventLogger> _logger;

    /// <summary>
    /// 构造函数，通过依赖注入获取必要的服务实例。
    /// </summary>
    /// <param name="cache">FusionCache 实例，由 DI 容器提供单例。</param>
    /// <param name="logger">针对当前类的日志记录器。</param>
    public FusionCacheEventLogger(
        IFusionCache cache,
        ILogger<FusionCacheEventLogger> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// 服务启动方法。
    /// 当应用程序启动时，此方法被调用。在此处注册所有需要监听的事件处理器。
    /// </summary>
    /// <param name="cancellationToken">用于通知启动操作应被取消的令牌。</param>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 订阅“缓存命中”事件 (Hit)。
        // 触发时机：当请求的数据在 L1 (内存) 或 L2 (分布式/Redis) 中找到有效数据时触发。
        // 语义：表示本次读取操作成功从缓存中获取了数据，无需回源。
        _cache.Events.Hit += OnHit;

        // 订阅“缓存未命中”事件 (Miss)。
        // 触发时机：当 L1 和 L2 中均不存在请求的 Key，或者数据已彻底过期且无法使用 Fail-Safe 时触发。
        // 语义：表示缓存中没有可用数据，FusionCache 随后通常会执行 Factory 委托去回源（查询数据库/API）。
        // 注意：Miss 不等于请求失败，也不等于数据库查询完成，它只是缓存层的状态反馈。
        _cache.Events.Miss += OnMiss;

        // 订阅“缓存写入”事件 (Set)。
        // 触发时机：当新的数据被成功存入缓存（无论是 L1 还是 L2）时触发。
        // 语义：表示 FusionCache 内部完成了写入操作。
        // 注意：不要将其命名为 "cache.loaded"，因为 "loaded" 容易让人误解为数据刚从数据库加载完成，而 Set 仅表示缓存层面的动作。
        _cache.Events.Set += OnSet;

        // 订阅“缓存移除”事件 (Remove)。
        // 触发时机：当显式调用 Remove/Evict 方法，或条目因过期/TTL 到达而被自动清理时触发。
        // 语义：表示缓存条目已被删除。通常发生在数据库更新成功后，主动失效缓存的场景。
        _cache.Events.Remove += OnRemove;

        // 订阅“Fail-Safe 激活”事件 (FailSafeActivate)。
        // 触发时机：当尝试从数据源（Factory）获取数据失败（异常或超时），且缓存中存在旧的可用数据时触发。
        // 语义：这是一个重要的警告信号。它表明后端数据源不稳定，但得益于 Fail-Safe 机制，系统降级返回了旧数据，保证了可用性。
        _cache.Events.FailSafeActivate += OnFailSafeActivate;

        // -----------------------------------------------------------------------------
        // 订阅 L1 (Memory) 层事件
        // L1 通常指进程内的内存缓存 (IMemoryCache)，速度最快，但仅限当前应用实例可见。
        // -----------------------------------------------------------------------------

        // 订阅 L1 内存缓存命中事件。
        // 触发条件：请求的 Key 在当前进程的内存缓存中存在且有效。
        // 意义：这是性能最好的情况，无需网络IO，无需序列化/反序列化。
        _cache.Events.Memory.Hit += OnMemoryHit;

        // 订阅 L1 内存缓存未命中事件。
        // 触发条件：请求的 Key 在当前进程的内存缓存中不存在或已过期。
        // 注意：L1 Miss 并不表示最终缓存未命中，因为系统接下来会尝试查询 L2 (Distributed)。
        // 意义：高频的 L1 Miss 可能意味着应用实例重启频繁、L1 容量不足或 TTL 设置过短。
        _cache.Events.Memory.Miss += OnMemoryMiss;


        // -----------------------------------------------------------------------------
        // 订阅 L2 (Distributed) 层事件
        // L2 通常指分布式缓存 (如 Redis, Memcached)，跨应用实例共享，涉及网络IO。
        // 只有当 L1 Miss 时，FusionCache 才会继续查询 L2。
        // -----------------------------------------------------------------------------

        // 订阅 L2 分布式缓存命中事件。
        // 触发条件：L1 未命中，但在分布式缓存（如 Redis）中找到了有效数据。
        // 后续行为：FusionCache 会将此数据回填 (Backfill) 到 L1 内存缓存中，以便下次请求直接命中 L1。
        // 意义：表示发生了“L1 穿透但 L2 命中”，性能次于 L1 Hit，但优于回源数据库。
        _cache.Events.Distributed.Hit += OnDistributedHit;

        // 订阅 L2 分布式缓存未命中事件。
        // 触发条件：L1 未命中，且分布式缓存中也不存在该 Key 或已过期。
        // 后续行为：这通常意味着“最终缓存未命中” (Final Miss)，FusionCache 将执行 Factory 委托去回源（如查询数据库）。
        // 意义：这是缓存穿透的直接信号。如果高频出现，需检查缓存策略或是否存在恶意请求。
        _cache.Events.Distributed.Miss += OnDistributedMiss;


        return Task.CompletedTask;
    }

    /// <summary>
    /// 服务停止方法。
    /// 当应用程序关闭时，此方法被调用。在此处取消所有事件订阅，防止内存泄漏或对象引用导致的 GC 问题。
    /// </summary>
    /// <param name="cancellationToken">用于通知停止操作应被取消的令牌。</param>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        // 取消订阅所有事件，确保垃圾回收器可以正常回收本实例及相关委托引用的对象。
        _cache.Events.Hit -= OnHit;
        _cache.Events.Miss -= OnMiss;
        _cache.Events.Set -= OnSet;
        _cache.Events.Remove -= OnRemove;
        _cache.Events.FailSafeActivate -= OnFailSafeActivate;

        _cache.Events.Memory.Hit -= OnMemoryHit;
        _cache.Events.Memory.Miss -= OnMemoryMiss;
        _cache.Events.Distributed.Hit -= OnDistributedHit;
        _cache.Events.Distributed.Miss -= OnDistributedMiss;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 处理缓存命中事件。
    /// </summary>
    /// <param name="sender">事件发起者（通常是 FusionCache 实例）。</param>
    /// <param name="args">包含命中详情的参数，如 Key 和数据状态。</param>
    private void OnHit(
        object? sender,
        FusionCacheEntryHitEventArgs args)
    {
        if (IsFusionCacheInternalKey(args.Key))
        {
            return;
        }

        // IsStale 指示返回的数据是否是“陈旧”的。
        // - false: 数据在 TTL 有效期内，完全新鲜。
        // - true: 数据已过 TTL，但因 Fail-Safe 或后台刷新机制而被返回。
        // 记录信息级别日志，用于统计缓存命中率和识别 stale read 情况。
        _logger.LogInformation("Event:{@event}, TraceId={@traceId},Msg:{@msg},CacheKey:{@cachekey}", LogEvents.CacheHit, Activity.Current?.Id, "Cache_Hit", args.Key);
    }

    /// <summary>
    /// 处理缓存未命中事件。
    /// </summary>
    /// <param name="sender">事件发起者。</param>
    /// <param name="args">包含未命中 Key 的参数。</param>
    private void OnMiss(
        object? sender,
        FusionCacheEntryEventArgs args)
    {
        if (IsFusionCacheInternalKey(args.Key))
        {
            return;
        }

        // Miss 只表示缓存没有可用数据。
        // 它不等于数据库查询完成，也不等于请求失败。
        // 高频 Miss 可能意味着缓存策略不合理、缓存预热不足或遭受缓存穿透攻击。
        _logger.LogInformation("Event:{@event}, TraceId={@traceId},Msg:{@msg},CacheKey:{@cachekey}", LogEvents.CacheMiss, Activity.Current?.Id, "Cache_Miss", args.Key);
    }

    /// <summary>
    /// 处理缓存写入事件。
    /// </summary>
    /// <param name="sender">事件发起者。</param>
    /// <param name="args">包含被写入 Key 的参数。</param>
    private void OnSet(
        object? sender,
        FusionCacheEntryEventArgs args)
    {
        if (IsFusionCacheInternalKey(args.Key))
        {
            return;
        }

        // Set 表示 FusionCache 已经写入缓存。
        // 不要把它命名为 cache.loaded，否则容易和数据库回源混淆。
        // 可用于追踪缓存更新频率，识别热点数据的刷新情况。
        _logger.LogInformation("Event:{@event}, TraceId={@traceId},Msg:{@msg},CacheKey:{@cachekey}", LogEvents.CacheSet, Activity.Current?.Id, "Cache_Set", args.Key);
    }

    /// <summary>
    /// 处理缓存移除事件。
    /// </summary>
    /// <param name="sender">事件发起者。</param>
    /// <param name="args">包含被移除 Key 的参数。</param>
    private void OnRemove(
        object? sender,
        FusionCacheEntryEventArgs args)
    {
        if (IsFusionCacheInternalKey(args.Key))
        {
            return;
        }

        // 记录缓存删除操作。
        // 通常用于审计缓存失效策略是否按预期执行，或在排查数据一致性问题时提供线索。
        _logger.LogInformation("Event:{@event}, TraceId={@traceId},Msg:{@msg},CacheKey:{@cachekey}", LogEvents.CacheRemove, Activity.Current?.Id, "Cache_Remove", args.Key);
    }

    /// <summary>
    /// 处理 Fail-Safe 激活事件。
    /// </summary>
    /// <param name="sender">事件发起者。</param>
    /// <param name="args">包含触发 Fail-Safe 的 Key 的参数。</param>
    private void OnFailSafeActivate(
        object? sender,
        FusionCacheEntryEventArgs args)
    {
        if (IsFusionCacheInternalKey(args.Key))
        {
            return;
        }

        // 记录警告级别日志。
        // 这是一个关键的运维信号。它意味着：
        // 1. 后端数据源（数据库/API）刚刚发生了错误或超时。
        // 2. 系统正在降级运行，返回旧数据以保证可用性。
        // 运维人员应监控此类日志的数量，若激增需立即检查后端服务健康状况。
        _logger.LogInformation("Event:{@event}, TraceId={@traceId},Msg:{@msg},CacheKey:{@cachekey}", LogEvents.CacheFailSafe, Activity.Current?.Id, "Cache_FailSafe", args.Key);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnMemoryHit(
        object? sender,
        FusionCacheEntryHitEventArgs args)
    {
        if (IsFusionCacheInternalKey(args.Key))
        {
            return;
        }

        // L1 命中说明数据来自当前应用实例内存。
        // 这是最快路径，不会访问 Redis，也不会查询数据库。
        _logger.LogDebug("Event:{@event}, TraceId={@traceId},Msg:{@msg},CacheKey:{@cachekey}", LogEvents.CacheMemoryHit, Activity.Current?.Id, "Cache_MemoryHit", args.Key);
    }

    private void OnMemoryMiss(
        object? sender,
        FusionCacheEntryEventArgs args)
    {
        if (IsFusionCacheInternalKey(args.Key))
        {
            return;
        }

        // L1 未命中不代表整个缓存未命中。
        // FusionCache 接下来还可能继续查询 Redis。
        //
        // 注意：
        // FusionCache 内部可能会多次检查 L1，
        // 因此一次业务请求可能产生多次 memory.miss。
        _logger.LogDebug("Event:{@event}, TraceId={@traceId},Msg:{@msg},CacheKey:{@cachekey}", LogEvents.CacheMemoryMiss, Activity.Current?.Id, "Cache_MemoryMiss", args.Key);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnDistributedHit(
        object? sender,
        FusionCacheEntryHitEventArgs args)
    {
        if (IsFusionCacheInternalKey(args.Key))
        {
            return;
        }

        // L2 命中说明数据来自 Redis。
        // FusionCache 通常会把 Redis 中的数据重新写入 L1，
        // 这样当前实例后续请求可以直接命中本地内存。
        _logger.LogDebug("Event:{@event}, TraceId={@traceId},Msg:{@msg},CacheKey:{@cachekey}", LogEvents.CacheDistributeHit, Activity.Current?.Id, "Cache_DistributedHit", args.Key);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnDistributedMiss(
        object? sender,
        FusionCacheEntryEventArgs args)
    {
        if (IsFusionCacheInternalKey(args.Key))
        {
            return;
        }

        // L2 未命中说明 Redis 中也没有可用数据。
        // 如果最终高层事件也是 cache.miss，
        // 后续通常会执行 Factory 查询数据库。
        _logger.LogDebug("Event:{@event}, TraceId={@traceId},Msg:{@msg},CacheKey:{@cachekey}", LogEvents.CacheDistributeMiss, Activity.Current?.Id, "Cache_DistributedMiss", args.Key);
    }

    /// <summary>
    /// 过滤FusionCache内部查询缓存
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private static bool IsFusionCacheInternalKey(string key)
    {
        return key.StartsWith("__fc:", StringComparison.Ordinal);
    }
}
