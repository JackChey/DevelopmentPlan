namespace Instructure.Caching;

public interface IAppCache
{
    /// <summary>
    /// 获取缓存；如果缓存不存在，则执行 factory 回源获取数据，并写入缓存。
    /// 
    /// 使用 ValueTask 是为了和 FusionCache API 保持一致。
    /// 调用方仍然可以正常 await：
    /// var result = await _cache.GetOrSetAsync(...);
    /// </summary>
    ValueTask<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CacheEntryPolicy? policy = null,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// 主动设置缓存。
    /// 常用于预热缓存，或者业务明确知道要写入某个缓存值的场景。
    /// </summary>
    ValueTask SetAsync<T>(
        string key,
        T value,
        CacheEntryPolicy? policy = null,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// 删除缓存。
    /// 写数据库成功后，应调用该方法删除相关缓存。
    /// 注意：这里通过 FusionCache 删除，而不是直接删 Redis，
    /// 这样 Backplane 才能通知其他节点清理本地一级缓存。
    /// </summary>
    ValueTask RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据缓存键读取缓存数据；只读取缓存，不回源、不写入缓存。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
}
