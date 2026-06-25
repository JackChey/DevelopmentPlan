namespace Instructure.Caching;

public sealed class CacheEnvelope<T>
{
    /// <summary>
    /// 表示这次查询是否有真实业务数据。
    /// false 代表数据库里没有查到数据。
    /// </summary>
    public bool HasValue { get; init; }

    /// <summary>
    /// 实际缓存的数据。生产中建议缓存 DTO，不缓存 EF Core Entity。
    /// </summary>
    public T? Value { get; init; }

    public static CacheEnvelope<T> FromValue(T value)
    {
        return new CacheEnvelope<T>
        {
            HasValue = true,
            Value = value
        };
    }

    public static CacheEnvelope<T> Null()
    {
        return new CacheEnvelope<T>
        {
            HasValue = false,
            Value = default
        };
    }
}
