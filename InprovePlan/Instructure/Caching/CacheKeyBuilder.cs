namespace Instructure.Caching;

using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public interface ICacheKeyBuilder
{
    string Build(string module, string name, params object[] parts);

    string BuildForQuery(string module, string name, object query);
}

public sealed class CacheKeyBuilder : ICacheKeyBuilder
{
    private readonly CacheOptions _options;

    public CacheKeyBuilder(IOptions<CacheOptions> options)
    {
        _options = options.Value;
    }

    public string Build(string module, string name, params object[] parts)
    {
        var keyParts = parts
            .Where(x => x is not null)
            .Select(x => x.ToString());

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

    public string BuildForQuery(string module, string name, object query)
    {
        // 查询条件可能很长，所以不要直接拼接整个 JSON。
        // 使用稳定 JSON + SHA256，避免 Key 太长，也避免遗漏查询条件。
        var json = JsonSerializer.Serialize(query);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        var hash = Convert.ToHexString(bytes).ToLowerInvariant();

        return Build(module, name, hash);
    }
}
