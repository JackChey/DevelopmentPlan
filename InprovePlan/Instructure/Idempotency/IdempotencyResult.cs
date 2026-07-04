namespace Instructure.Idempotency;

/// <summary>
/// 表示幂等性检查和处理的结果。
/// </summary>
/// <remarks>
/// 该类用于向调用者（如中间件或控制器）传达当前请求在幂等性流程中的状态，
/// 以及是否需要返回缓存的响应数据。
/// </remarks>
public sealed class IdempotencyResult
{
    /// <summary>
    /// 获取幂等性处理的状态。
    /// </summary>
    /// <value>
    /// 指示当前请求是首次处理、正在处理、命中缓存还是发生冲突。
    /// </value>
    public IdempotencyResultState State { get; init; }

    /// <summary>
    /// 获取缓存的 HTTP 响应体内容。
    /// </summary>
    /// <value>
    /// 当 <see cref="State"/> 为 <see cref="IdempotencyResultState.Cached"/> 时，
    /// 此字段包含之前请求的响应 payload。
    /// 其他状态下通常为 null。
    /// </value>
    public object? CachedResponse { get; init; }

    /// <summary>
    /// 获取缓存的 HTTP 响应状态码。
    /// </summary>
    /// <value>
    /// 当 <see cref="State"/> 为 <see cref="IdempotencyResultState.Cached"/> 时，
    /// 此字段包含之前成功处理该请求时返回的状态码（如 200, 201 等）。
    /// 其他状态下通常为 null。
    /// </value>
    public int? ResponseStatusCode { get; init; }

    /// <summary>
    /// 创建一个表示“请求已开始记录但尚未完成”的结果实例。
    /// </summary>
    /// <returns>
    /// 状态为 <see cref="IdempotencyResultState.Started"/> 的新实例。
    /// </returns>
    /// <remarks>
    /// 通常在检测到新请求并准备在存储中创建幂等性记录时调用，
    /// 用于防止并发请求同时进入业务逻辑处理阶段。
    /// </remarks>
    public static IdempotencyResult Started() => new()
    {
        State = IdempotencyResultState.Started
    };

    /// <summary>
    /// 创建一个表示“命中缓存”的结果实例。
    /// </summary>
    /// <param name="statusCode">之前请求返回的 HTTP 状态码。</param>
    /// <param name="body">之前请求返回的响应体内容。</param>
    /// <returns>
    /// 状态为 <see cref="IdempotencyResultState.Cached"/> 且包含响应数据的新实例。
    /// </returns>
    /// <remarks>
    /// 当系统发现相同的幂等性 Key 已经处理完成时调用。
    /// 调用方应直接返回此结果中的状态码和 body，而不再执行业务逻辑。
    /// </remarks>
    public static IdempotencyResult Cached(object cachedResponse) => new()
    {
        State = IdempotencyResultState.Cached,
        CachedResponse = cachedResponse,
    };

    /// <summary>
    /// 创建一个表示“请求正在处理中”的结果实例。
    /// </summary>
    /// <returns>
    /// 状态为 <see cref="IdempotencyResultState.Processing"/> 的新实例。
    /// </returns>
    /// <remarks>
    /// 当检测到另一个具有相同 Key 的请求正在进行中（例如被锁住或异步处理未完成）时调用。
    /// 调用方通常应返回 409 Conflict 或 429 Too Many Requests，或者让客户端稍后重试。
    /// </remarks>
    public static IdempotencyResult Processing() => new()
    {
        State = IdempotencyResultState.Processing
    };

    /// <summary>
    /// 创建一个表示“冲突”的结果实例。
    /// </summary>
    /// <returns>
    /// 状态为 <see cref="IdempotencyResultState.Conflict"/> 的新实例。
    /// </returns>
    /// <remarks>
    /// 当请求违反幂等性约束时调用（例如：相同的 Key 但请求体哈希值不同，
    /// 或者在不允许重放的状态下再次提交）。
    /// 调用方应返回 409 Conflict 错误。
    /// </remarks>
    public static IdempotencyResult Conflict() => new()
    {
        State = IdempotencyResultState.Conflict
    };
}

