namespace Instructure.Caching;

public sealed class CacheEntryPolicy
{
    /// <summary>
    /// 正常数据缓存时间。
    /// </summary>
    public TimeSpan Duration { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 空结果缓存时间。
    /// </summary>
    public TimeSpan NullValueDuration { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 是否缓存空结果。
    /// 对详情查询建议开启，对强实时查询谨慎开启。
    /// </summary>
    public bool CacheNullValue { get; init; } = true;
}
