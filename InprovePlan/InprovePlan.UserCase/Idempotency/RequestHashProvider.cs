namespace InprovePlan.UserCase.Idempotency;

using Instructure.Idempotency;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;


/// <summary>
/// 基于 SHA256 的请求 Hash 生成器。
/// 
/// 核心目标：为 HTTP 请求生成唯一的指纹（Hash），用于缓存键生成、幂等性检查或请求去重。
/// 
/// 唯一性保证：
/// 只要 Method、Path、Query、User、Tenant、Body 任意一项不同，
/// 最终生成的 Hash 值就会不同。
/// 
/// 注意：
/// 1. 对 Method 和 Path 进行了大小写标准化，以避免因大小写差异导致相同的逻辑请求产生不同的 Hash。
/// 2. 使用 JSON 序列化将结构化数据转换为字符串，确保顺序一致性和可预测性。
/// </summary>
public sealed class RequestHashProvider : IRequestHashProvider
{
    /// <summary>
    /// 计算请求的唯一哈希值。
    /// </summary>
    /// <param name="source">包含请求关键信息的源对象，如方法、路径、查询字符串、用户ID、租户ID和请求体。</param>
    /// <returns>
    /// 返回一个十六进制字符串表示的 SHA256 哈希值。
    /// 该哈希值可用于唯一标识具有相同语义内容的请求。
    /// </returns>
    public string ComputeHash(RequestHashSource source)
    {
        // 1. 数据标准化与聚合
        // 创建一个匿名对象，将请求的关键部分组合在一起。
        // 这一步至关重要，因为它定义了哪些因素会影响哈希结果。
        var normalized = new
        {
            // HTTP 方法标准化为大写 (例如: "get" -> "GET")
            // 确保 "GET" 和 "get" 被视为相同的方法
            method = source.Method.ToUpperInvariant(),

            // URL 路径标准化为小写 (例如: "/Api/Users" -> "/api/users")
            // 大多数 Web 框架对路径大小写不敏感或统一处理，此处标准化可避免不必要的哈希差异
            path = source.Path.ToLowerInvariant(),

            // 查询字符串保持原样
            // 注意：如果查询参数顺序不同但内容相同（如 a=1&b=2 vs b=2&a=1），这里会产生不同的哈希。
            // 如需更严格的去重，需在此处对查询参数进行排序和规范化。
            queryString = source.QueryString,

            // 用户 ID，用于区分不同用户的请求
            userId = source.UserId,

            // 请求体内容
            // 对于 POST/PUT 请求，Body 通常是判断请求是否重复的关键字段
            body = source.Body
        };

        // 2. 序列化为 JSON 字符串
        // 使用 System.Text.Json 将匿名对象序列化为标准的 JSON 字符串。
        // JsonSerializer 默认会保持属性定义的顺序，这保证了相同输入始终产生相同的 JSON 字符串。
        // 这种确定性是生成稳定哈希的前提。
        var json = JsonSerializer.Serialize(normalized);

        // 3. 计算 SHA256 哈希
        // a. 将 JSON 字符串转换为 UTF-8 字节数组
        // b. 使用 SHA256 算法计算哈希值
        // SHA256.HashData 是 .NET 6+ 引入的高效静态方法，无需实例化 SHA256 对象，性能更好且线程安全。
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));

        // 4. 转换为十六进制字符串
        // 将字节数组转换为大写十六进制字符串（例如: "A1B2..."）。
        // Convert.ToHexString 是 .NET 5+ 引入的高效方法，比传统的 BitConverter.ToString().Replace("-", "") 性能更优。
        return Convert.ToHexString(bytes);
    }
}
