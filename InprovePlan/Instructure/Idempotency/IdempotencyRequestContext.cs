namespace Instructure.Idempotency;

/// <summary>
/// 表示幂等性请求的上下文信息。
/// </summary>
/// <remarks>
/// 此类用于封装识别唯一请求所需的关键数据，通常配合幂等性中间件或服务使用，
/// 以防止网络重试、客户端重复提交等情况导致的数据不一致或重复操作。
/// </remarks>
public sealed class IdempotencyRequestContext
{
    /// <summary>
    /// 获取幂等性键（Idempotency Key）。
    /// </summary>
    /// <value>
    /// 由客户端生成的唯一标识符（如 GUID），用于区分不同的业务请求。
    /// 相同的 Key 在特定时间窗口内应被视为同一请求。
    /// </value>
    public required string Key { get; init; }

    /// <summary>
    /// 获取请求内容的哈希值。
    /// </summary>
    /// <value>
    /// 对请求体（Body）或关键参数进行哈希计算后的字符串。
    /// 用于验证具有相同 Key 的请求是否具有完全相同的内容，防止重放攻击或参数篡改。
    /// </value>
    public required string RequestHash { get; init; }

    /// <summary>
    /// 获取发起请求的用户标识。
    /// </summary>
    /// <value>
    /// 当前请求所属用户的 ID。用于隔离不同用户的幂等性记录，
    /// 确保不同用户即使使用相同的 Key 也不会发生冲突。
    /// </value>
    public required long UserId { get; init; }

    /// <summary>
    /// 获取 HTTP 请求方法。
    /// </summary>
    /// <value>
    /// 例如 "POST", "PUT", "PATCH" 等。
    /// 通常幂等性检查主要针对非 GET 请求，此字段用于辅助判断请求类型。
    /// </value>
    public required string Method { get; init; }

    /// <summary>
    /// 获取 HTTP 请求路径。
    /// </summary>
    /// <value>
    /// 请求的 URL 路径（例如 "/api/orders"）。
    /// 结合 Method 和 Key，可以更精确地定位幂等性资源的范围。
    /// </value>
    public required string Path { get; init; }
}
