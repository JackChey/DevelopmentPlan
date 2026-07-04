using Instructure.Idempotency;

namespace Instructure.Interfaces;

/// <summary>
/// 幂等服务接口。
/// 
/// 该服务负责：
/// 1. 开始一次幂等请求
/// 2. 判断是否首次请求
/// 3. 判断是否命中缓存
/// 4. 判断是否请求冲突
/// 5. 保存成功响应
/// 6. 标记失败状态
/// 
/// 注意：
/// 该接口不应该依赖 HTTP。
/// 它应该只面向业务请求上下文工作。
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// 开始一次幂等请求。
    /// 
    /// 如果是首次请求，返回 Started。
    /// 如果之前已经成功，返回 Cached。
    /// 如果正在处理中，返回 Processing。
    /// 如果 Key 相同但请求内容不同，返回 Conflict。
    /// </summary>
    Task<IdempotencyResult> BeginAsync(
        IdempotencyRequestContext context,
        Type responseType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记幂等请求成功，并保存完整业务响应。
    /// 
    /// 重复请求命中缓存时，会直接反序列化该响应并返回。
    /// </summary>
    Task CompleteAsync(
        IdempotencyRequestContext context,
        Instructure.IResult.IResult response,
        Type responseType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记幂等请求失败。
    /// 
    /// 用于 Handler 抛异常的场景。
    /// </summary>
    Task FailAsync(
        IdempotencyRequestContext context,
        Exception exception,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记幂等请求失败。
    /// 
    /// 用于业务返回 IsSuccess = false 的场景。
    /// </summary>
    Task FailAsync(
        IdempotencyRequestContext context,
        string errorMessage,
        CancellationToken cancellationToken = default);
}

