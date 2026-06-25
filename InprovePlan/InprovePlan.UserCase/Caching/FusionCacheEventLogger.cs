namespace InprovePlan.UserCase.Caching;

using Instructure.Exceptions;
using Instructure.SystemLogs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Events;

/// <summary>
/// FusionCache 事件监控与日志记录服务。
/// 
/// 该类实现了 IHostedService 接口，作为后台服务随应用程序启动和停止。
/// 主要职责是订阅 FusionCache 的核心生命周期事件，并将关键操作记录到日志系统中。
/// 
/// 作用：
/// 1. 可观测性：帮助开发人员了解缓存的命中/未命中比率。
/// 2. 故障诊断：通过 FailSafeActivate 事件及时发现后端数据源（如数据库、Redis）的不稳定情况。
/// 3. 行为审计：追踪缓存的写入和删除操作，辅助排查数据一致性问题。
/// </summary>
public sealed class FusionCacheEventLogger : IHostedService
{
    // FusionCache 实例引用。
    // 用于访问其 Events 属性以订阅/取消订阅缓存生命周期事件。
    private readonly IFusionCache _cache;

    // 日志记录器实例。
    // 用于将缓存事件输出到 configured logging providers (如 Console, File, Seq 等)。
    private readonly ILogger<FusionCacheEventLogger> _logger;

    /// <summary>
    /// 构造函数，通过依赖注入获取必要的服务实例。
    /// </summary>
    /// <param name="cache">FusionCache 实例，由 DI 容器提供单例。</param>
    /// <param name="logger">针对当前类的日志记录器。</param>
    public FusionCacheEventLogger(
        IFusionCache cache,
        ILogger<FusionCacheEventLogger> logger
        )
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
        // 订阅“缓存命中”事件。
        // 触发时机：当请求的数据在 L1 (内存) 或 L2 (分布式/Redis) 中找到时触发。
        // 注意：即使数据逻辑上已过期但处于 Fail-Safe 窗口期内，也可能触发此事件（需结合 IsStale 判断）。
        _cache.Events.Hit += OnHit;

        // 订阅“缓存未命中”事件。
        // 触发时机：当 L1 和 L2 中均不存在请求的 Key，或者数据已彻底过期且无法使用 Fail-Safe 时触发。
        // 意义：高频率的 Miss 可能意味着缓存策略不合理或遭受缓存穿透攻击。
        _cache.Events.Miss += OnMiss;

        // 订阅“缓存写入”事件。
        // 触发时机：当新的数据被成功存入缓存（无论是 L1 还是 L2）时触发。
        // 意义：用于监控缓存更新频率，识别热点数据的刷新情况。
        _cache.Events.Set += OnSet;

        // 订阅“缓存移除”事件。
        // 触发时机：当显式调用 Remove/Evict 方法，或条目因过期/TTL 到达而被自动清理时触发。
        // 意义：辅助分析缓存失效策略是否按预期工作。
        _cache.Events.Remove += OnRemove;

        // 订阅“Fail-Safe 激活”事件。
        // 触发时机：当尝试从数据源（Factory）获取数据失败（异常或超时），且缓存中存在旧的可用数据时触发。
        // 意义：这是系统健康的重要指标。频繁触发表明后端服务（DB/API）存在稳定性问题，但得益于 Fail-Safe，用户请求并未失败。
        _cache.Events.FailSafeActivate += OnFailSafeActivate;

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
        // 记录调试级别日志。
        // Key: 命中的缓存键。
        // IsStale: 指示返回的数据是否是“陈旧”的。
        //   - false: 数据在 TTL 有效期内，完全新鲜。
        //   - true: 数据已过 TTL，但因 Fail-Safe 或后台刷新机制而被返回。

        _logger.LogInformation("Event:{@event}, TraceId={@traceId},Msg:{@msg},CacheKey:{@cachekey}", LogEvents.CacheHit, Activity.Current?.Id , "Cache_Hit", args.Key);
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
        _logger.LogInformation("Event:{@event}, TraceId={@traceId},Msg:{@msg},CacheKey:{@cachekey}", LogEvents.CacheMiss,  Activity.Current?.Id , "Cache_Miss", args.Key );
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
        _logger.LogInformation("Event:{@event}, TraceId={@traceId},Msg:{@msg},CacheKey:{@cachekey}", LogEvents.CacheLoaded, Activity.Current?.Id, "Cache_Loaded", args.Key);
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
        // 记录信息级别日志。
        // 相比读写，删除操作通常较少发生，提升日志级别有助于在海量 Debug 日志中快速定位失效操作。
        _logger.LogInformation("Event:{@event}, TraceId={@traceId},Msg:{@msg},CacheKey:{@cachekey}", LogEvents.CacheRemove,Activity.Current?.Id, "Cache_Remove", args.Key);
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
        // 记录警告级别日志。
        // 这是一个关键的运维信号。它意味着：
        // 1. 后端数据源（数据库/API）刚刚发生了错误或超时。
        // 2. 系统正在降级运行，返回旧数据以保证可用性。
        // 运维人员应监控此类日志的数量，若激增需立即检查后端服务健康状况。
        _logger.LogInformation("Event:{@event}, TraceId={@traceId},Msg:{@msg},CacheKey:{@cachekey}", LogEvents.CacheFailSafe, Activity.Current?.Id, "Cache_FailSafe", args.Key);
    }
}

