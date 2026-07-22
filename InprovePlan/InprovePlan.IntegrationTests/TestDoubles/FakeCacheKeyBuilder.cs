using Instructure.Caching;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InprovePlan.IntegrationTests.TestDoubles;

/// <summary>
/// 伪造的缓存键构建器实现，用于生成标准化的缓存键。
/// 实现了 ICacheKeyBuilder 接口，提供基于应用配置和查询内容的缓存键生成逻辑。
/// </summary>
public sealed class FakeCacheKeyBuilder : ICacheKeyBuilder
{
    /// <summary>
    /// 缓存配置选项，包含应用名称、环境、键版本等基础信息。
    /// </summary>
    private readonly CacheOptions _options;

    /// <summary>
    /// 初始化 <see cref="FakeCacheKeyBuilder"/> 类的新实例。
    /// </summary>
    /// <param name="options">缓存配置选项，用于获取应用名称、环境和键版本等全局前缀信息。</param>
    public FakeCacheKeyBuilder(CacheOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// 构建缓存键。
    /// 将应用名称、环境、模块名、操作名、键版本以及额外的参数部分拼接成一个以冒号分隔的字符串。
    /// 忽略参数数组中的 null 值。
    /// </summary>
    /// <param name="module">模块名称，用于区分不同的业务模块。</param>
    /// <param name="name">操作或资源名称，用于区分模块内的具体操作。</param>
    /// <param name="parts">可变数量的额外参数部分，将转换为字符串并追加到键的末尾。null 值将被过滤掉。</param>
    /// <returns>生成的完整缓存键字符串。</returns>
    public string Build(string module, string name, params object[] parts)
    {
        // 过滤掉 null 值，并将剩余对象转换为字符串表示
        var keyParts = parts
            .Where(x => x is not null)
            .Select(x => x.ToString());

        // 拼接基础部分（应用名、环境、模块、名称、键版本）和额外参数部分，使用冒号作为分隔符
        return string.Join(":",
            new[]
            {
                _options.AppName,
                _options.Environment,
                module,
                name,
                _options.KeyVersion
            }.Concat(keyParts!));
    }

    /// <summary>
    /// 为查询对象构建缓存键。
    /// 为了避免因查询条件过长导致缓存键过长，先将查询对象序列化为 JSON，
    /// 然后计算其 SHA256 哈希值，最后将哈希值作为额外参数调用 Build 方法生成缓存键。
    /// 这种方式既保证了键的唯一性和稳定性，又控制了键的长度。
    /// </summary>
    /// <param name="module">模块名称，用于区分不同的业务模块。</param>
    /// <param name="name">操作或资源名称，用于区分模块内的具体操作。</param>
    /// <param name="query">查询条件对象，将被序列化并哈希处理。</param>
    /// <returns>生成的包含查询哈希值的缓存键字符串。</returns>
    public string BuildForQuery(string module, string name, object query)
    {
        // 查询条件可能很长，所以不要直接拼接整个 JSON。
        // 使用稳定 JSON + SHA256，避免 Key 太长，也避免遗漏查询条件。

        // 将查询对象序列化为 JSON 字符串
        var json = JsonSerializer.Serialize(query);

        // 计算 JSON 字符串的 UTF8 字节数组的 SHA256 哈希值
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));

        // 将哈希字节数组转换为小写十六进制字符串
        var hash = Convert.ToHexString(bytes).ToLowerInvariant();

        // 使用模块、名称和哈希值构建最终的缓存键
        return Build(module, name, hash);
    }
}

