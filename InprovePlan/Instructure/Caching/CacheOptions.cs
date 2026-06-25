namespace Instructure.Caching;

public sealed class CacheOptions
{
    /// <summary>
    /// 应用名称，用于区分不同系统的缓存 Key。
    /// </summary>
    public string AppName { get; init; } = "defaultappname";

    /// <summary>
    /// 当前环境，例如 dev、test、prod。
    /// 避免测试环境误读生产缓存。
    /// </summary>
    public string Environment { get; init; } = "dev";

    /// <summary>
    /// Key 版本。DTO 字段结构变化时，可以升级版本整体隔离旧缓存。
    /// </summary>
    public string KeyVersion { get; init; } = "v1";

    /// <summary>
    /// 默认正常数据缓存时间。
    /// </summary>
    public int DefaultDurationSeconds { get; init; } = 300;

    /// <summary>
    /// 空结果缓存时间，用来防止缓存穿透。
    /// 不宜太长，否则刚创建的数据短时间内可能仍读到空结果。
    /// </summary>
    public int NullValueDurationSeconds { get; init; } = 60;

    /// <summary>
    /// 随机抖动最大秒数，用来避免大量 Key 同时过期。
    /// </summary>
    public int JitterMaxSeconds { get; init; } = 30;
}