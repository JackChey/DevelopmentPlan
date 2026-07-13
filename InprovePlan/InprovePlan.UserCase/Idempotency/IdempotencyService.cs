namespace InprovePlan.UserCase.Idempotency;

using Instructure.Caching;
using Instructure.Exceptions;
using Instructure.Idempotency;
using Instructure.Interfaces;
using Instructure.IResult;
using Instructure.Repositories;
using Instructure.SystemLogs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Net;
using System.Text.Json;


/// <summary>
/// 幂等服务实现。
/// 
/// 该类是幂等机制的核心实现。
/// 
/// 它的主要职责：
/// 1. 使用分布式锁降低并发冲突。
/// 2. 使用数据库唯一索引保证最终正确性。
/// 3. 判断同一个 Idempotency-Key 是否已经处理过。
/// 4. 判断同一个 Idempotency-Key 是否被用于不同请求参数。
/// 5. 保存第一次成功请求的响应结果。
/// 6. 后续重复请求直接返回第一次响应。
/// 
/// 注意：
/// 分布式锁只是优化，数据库唯一索引才是最终兜底。
/// </summary>
public sealed class IdempotencyService(
    IRepository<IdempotencyRecord> _repository,
    IOptions<IdempotencyOptions> configureOptions,
    IDistributedLock _distributedLock,
    ILogger<IdempotencyService> _logger,
    ICacheKeyBuilder _keyBuilder,
    IRedisRepository _redisRepository,
    IIdempotencyRecordRepository _idempotencyRepository,
    IHttpContextAccessor httpContextAccessor,
    IIdGenerator idGenerator
    ) : IIdempotencyService
{

    // 幂等性配置选项，包含锁超时时间、记录过期时间等。
    private readonly IdempotencyOptions _options = configureOptions.Value;


    /// <summary>
    /// 开始幂等性检查流程。
    /// 
    /// 执行逻辑：
    /// 1. 尝试获取 Redis 分布式锁，避免大量相同 Key 的请求同时穿透到数据库。
    /// 2. 若获锁成功，尝试在数据库中插入一条状态为 Processing 的记录。
    /// 3. 若插入成功，视为首次请求，允许业务继续。
    /// 4. 若插入失败（唯一索引冲突），查询现有记录状态并返回相应结果。
    /// </summary>
    /// <param name="context">请求上下文，包含幂等键、用户信息等。</param>
    /// <param name="responseType">请求处理结果。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>幂等性检查结果，决定后续流程走向。</returns>
    public async Task<IdempotencyResult> BeginAsync(
        IdempotencyRequestContext context,
        Type responseType,
        CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(responseType);

        // 构建唯一的锁 Key，通常由用户ID和业务幂等键组成，确保隔离性。
        var recordKey = _keyBuilder.Build(
            module: "idempotencyservice",
            name: "record",
            context.Key, context.UserId);

        // 校对 幂等键 是否已存在
        var existingRecord = await _redisRepository.GetAsync<IdempotencyRecord>(recordKey, cancellationToken);

        // 存在则返回结果
        if (existingRecord is not null)
        {
            return ReturnRecord(existingRecord, context, responseType);
        }

        // 构建唯一的分布式锁 Key，通常由用户ID和业务幂等键组成，确保隔离性。
        var lockKey = _keyBuilder.Build(
            module: "idempotencyservice",
            name: "key",
            context.Key, context.UserId);

        // 尝试获取分布式锁。
        // LockSeconds 通常设置较短（如 3-5 秒），仅用于阻挡瞬间的高并发重复提交。
        // 使用 await using 确保锁在使用完毕后自动释放（即使发生异常）。
        await using var distributedLock = await _distributedLock.TryAcquireAsync(
            lockKey,
            TimeSpan.FromSeconds(_options.LockSeconds),
            cancellationToken);

        // 如果锁获取失败，说明此刻有另一个相同 Key 的请求正在处理中（或刚进入处理流程）。
        // 直接返回 "Processing" 状态，告诉客户端/调用方稍后重试或等待，无需再查库。
        if (distributedLock is null)
        {
            return IdempotencyResult.Processing();
        }

        // 3. 获取锁后再查一次缓存，避免重复插入。
        existingRecord = await _redisRepository.GetAsync<IdempotencyRecord>(recordKey, cancellationToken);

        if (existingRecord is not null)
        {
            // 同上处理 cached 状态
            return ReturnRecord(existingRecord, context, responseType);
        }

        // --- 第二步：数据库记录创建（最终防线） ---

        var now = DateTimeOffset.UtcNow;

        // 构造新的幂等记录实体。
        // 初始状态设为 Processing，表示该请求正在处理中，尚未完成。
        var record = new IdempotencyRecord
        {
            Id = idGenerator.NewId(),
            Key = context.Key,             // 业务幂等键
            RequestHash = context.RequestHash, // 请求内容的哈希值，用于检测参数是否篡改
            UserId = context.UserId,
            Method = context.Method,       // HTTP 方法
            Path = context.Path,           // 请求路径
            Status = IdempotencyRecordStatus.Processing, // 初始状态
            CreatedByUserId = context.UserId,
            ExpiresAt = now.AddHours(_options.ExpirationHours) // 设置记录过期时间，避免数据无限膨胀
        };

        // 尝试插入 Processing 记录。
        //
        // 如果插入成功，说明这是第一次请求。
        // 如果插入失败，大概率是数据库唯一索引冲突，说明已有相同 Key。
        //
        // 注意：
        // 即使有分布式锁，也必须保留数据库唯一索引。
        // 因为 Redis/锁服务不可用、锁过期、服务重启等情况下，最终仍要靠数据库兜底。
        var created = await _idempotencyRepository.TryCreateProcessingAsync(record, cancellationToken);

        if (created)
        {
            await _redisRepository.SetAsync(recordKey, record, TimeSpan.FromSeconds(_options.LockSeconds), cancellationToken);

            // 记录创建成功，返回 Started，允许中间件放行至后续业务逻辑。
            return IdempotencyResult.Started();
        }

        // --- 第三步：处理插入失败的情况（重复请求） ---

        // 插入失败意味着数据库中已存在相同 Key 的记录。
        // 需要查询该记录以决定如何响应。
        var newexistingRecord = await _idempotencyRepository.FirstOrDefaultAsync(
                   order => order.UserId == context.UserId && order.Key == context.Key,
                   cancellationToken);

        if (newexistingRecord is null)
        {
            // 理论上不应该出现。
            // 可能原因：
            // 1. 插入冲突后记录刚好被清理任务删除。
            // 2. 数据库读写延迟。
            // 3. Repository 实现异常。
            //
            // 这里保守返回 Processing，避免重复执行业务。
            var http = new LogHttpRequestInfo()
            {
                Route = httpContext.Request.Path,
                Method = httpContext.Request.Method,
                StatusCode = httpContext.Response.StatusCode,
                ClientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unkown",
            };

            httpContext.Items.TryGetValue("auth", out var auth);

            _logger.LogWarning("Event:{@event},Http:{@http},Auth:{@auth},ErrorCode:{@errorcode},TraceId={@traceId},Msg:{@msg}, Key: {Key}", LogEvents.IdempotencyKeyDisappeared, http, auth, "Idempotency.key.disappeared", Activity.Current?.Id ?? httpContext.TraceIdentifier, "Idempotency request failed during processing.", context.Key);

            return IdempotencyResult.Processing();
        }
        else
        {

        }

        // 【安全性检查】请求内容一致性校验
        // 如果 Key 相同，但请求参数的 Hash 值不同，说明用户试图用同一个幂等键发送不同的业务数据。
        // 这通常是客户端错误或恶意攻击，必须拒绝。
        if (!string.Equals(newexistingRecord.RequestHash, context.RequestHash, StringComparison.Ordinal))
        {
            return IdempotencyResult.Conflict();
        }

        // --- 第四步：根据现有记录状态返回结果 ---
        return ReturnRecord(newexistingRecord, context, responseType);
    }

    /// <summary>
    /// 标记幂等请求成功。
    /// 
    /// 业务 Handler 执行成功后调用。
    /// 
    /// 这里会保存完整 IResult 响应。
    /// 后续重复请求命中缓存时，可以直接返回第一次响应。
    /// </summary>
    public async Task CompleteAsync(
        IdempotencyRequestContext context,
        IResult response,
        Type responseType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(responseType);

        // 使用 responseType 序列化，而不是 response.GetType()。
        //
        // 原因：
        // MediatR 的 TResponse 是调用方期望的响应类型。
        // 保存和读取使用同一个 Type，可以避免接口/派生类型序列化不一致。

        var recordKey = _keyBuilder.Build(
           module: "idempotencyservice",
           name: "record",
           context.Key, context.UserId);

        // 调用仓库方法更新状态和结果。
        // 注意：这里通常不需要再加锁，因为 Key 的唯一性已经保证了记录的独占性，
        // 且只有持有“首次请求”资格的流程才会执行到这里。
        var result = await _repository.FirstOrDefaultAsync(
                   order => order.UserId == context.UserId && order.Key == context.Key,
                   cancellationToken);

        if (result is null)
        {
            return;
        }

        var responseBody = JsonSerializer.Serialize(
            response,
            responseType);

        result.Status = IdempotencyRecordStatus.Succeeded;
        result.ResponseBody = responseBody;
        result.ResponseStatusCode = (int?)response.Status;
        result.CompletedAt = DateTimeOffset.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);

        // Redis 只是加速层，写失败不能影响主流程。
        try
        {
            await _redisRepository.SetAsync(
                recordKey,
                result,
                TimeSpan.FromHours(_options.ExpirationHours),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to write idempotency cache after request completed. Key: {Key}, UserId: {UserId}",
                context.Key,
                context.UserId);
        }
    }

    /// <summary>
    /// 标记请求处理失败。
    /// 
    /// 此方法在业务逻辑抛出未捕获异常时调用。
    /// 主要作用：
    /// 1. 记录错误日志，便于排查问题。
    /// 2. 更新数据库记录状态为 "Failed"（或根据策略删除/保留），
    ///    确保后续的重复请求不会被误判为“已完成”，从而允许客户端重试。
    /// </summary>
    /// <param name="context">请求上下文。</param>
    /// <param name="exception">导致失败的异常对象。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task FailAsync(
        IdempotencyRequestContext context,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;

        var result = await _repository.FirstOrDefaultAsync(
                   order => order.UserId == context.UserId && order.Key == context.Key,
                   cancellationToken);

        if (result is null)
        {
            return;
        }

        result.Status = IdempotencyRecordStatus.Failed;
        result.CompletedAt = DateTimeOffset.UtcNow;
        result.ResponseStatusCode = (int?)HttpStatusCode.ExpectationFailed;
        result.ErrorMessage = exception.Message;

        await _repository.SaveChangesAsync(cancellationToken);

        var recordKey = _keyBuilder.Build(
           module: "idempotencyservice",
           name: "record",
           context.Key, context.UserId);

        // 记录详细的错误日志，包含关键标识信息，方便追踪特定的幂等请求失败原因。
        var http = new LogHttpRequestInfo()
        {
            Route = httpContext.Request.Path,
            Method = httpContext.Request.Method,
            StatusCode = httpContext.Response.StatusCode,
            ClientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unkown",
        };

        httpContext.Items.TryGetValue("auth", out var auth);

        _logger.LogError(exception, "Event:{@event},Http:{@http},Auth:{@auth},ErrorCode:{@errorcode},TraceId={@traceId},Msg:{@msg}, Key: {Key}", LogEvents.IdempotencyFail, http, auth, "Idempotency.request.failed", Activity.Current?.Id ?? httpContext.TraceIdentifier, "Idempotency request failed during processing.", context.Key);

        // 调用仓库方法标记失败。
        // 具体实现中，MarkFailedAsync 可能会将状态设为 Failed，
        // 或者如果策略是“失败不缓存”，可能会直接删除该记录以允许完全重试。
        await _redisRepository.RemoveAsync(recordKey, cancellationToken);

    }

    /// <summary>
    /// 标记幂等请求失败。
    /// 
    /// 该方法用于业务没有抛异常，但返回 IsSuccess = false 的场景。
    /// </summary>
    public async Task FailAsync(
        IdempotencyRequestContext context,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;

        var result = await _repository.FirstOrDefaultAsync(
                   order => order.UserId == context.UserId && order.Key == context.Key,
                   cancellationToken);

        if (result is null)
        {
            return;
        }

        result.Status = IdempotencyRecordStatus.Failed;
        result.CompletedAt = DateTimeOffset.UtcNow;
        result.ResponseStatusCode = (int?)HttpStatusCode.ExpectationFailed;
        result.ErrorMessage = errorMessage;

        await _repository.SaveChangesAsync(cancellationToken);

        var recordKey = _keyBuilder.Build(
           module: "idempotencyservice",
           name: "record",
           context.Key, context.UserId);

        // 记录详细的错误日志，包含关键标识信息，方便追踪特定的幂等请求失败原因。
        var http = new LogHttpRequestInfo()
        {
            Route = httpContext.Request.Path,
            Method = httpContext.Request.Method,
            StatusCode = httpContext.Response.StatusCode,
            ClientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unkown",
        };

        httpContext.Items.TryGetValue("auth", out var auth);

        _logger.LogError("Event:{@event},Http:{@http},Auth:{@auth},ErrorCode:{@errorcode},TraceId={@traceId},Msg:{@msg}, Key: {Key}", LogEvents.IdempotencyFail, http, auth, "Idempotency.request.failed", Activity.Current?.Id ?? httpContext.TraceIdentifier, "Idempotency request failed during processing.", context.Key);

        // 调用仓库方法标记失败。
        // 具体实现中，MarkFailedAsync 可能会将状态设为 Failed，
        // 或者如果策略是“失败不缓存”，可能会直接删除该记录以允许完全重试。
        await _redisRepository.RemoveAsync(recordKey, cancellationToken);
    }

    /// <summary>
    /// 标记幂等请求失败。
    /// 
    /// 该方法用于业务没有抛异常，但返回 IsSuccess = false 的场景。
    /// </summary>
    public IdempotencyResult ReturnRecord(
        IdempotencyRecord record,
        IdempotencyRequestContext context,
        Type responseType,
        CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;

        // 【安全性检查】请求内容一致性校验
        // 如果 Key 相同，但请求参数的 Hash 值不同，说明用户试图用同一个幂等键发送不同的业务数据。
        // 这通常是客户端错误或恶意攻击，必须拒绝。
        if (!string.Equals(record.RequestHash, context.RequestHash, StringComparison.Ordinal))
        {
            return IdempotencyResult.Conflict();
        }

        // 如果之前已经成功，则读取保存的响应并反序列化成原始 TResponse。
        if (record.Status == IdempotencyRecordStatus.Succeeded)
        {
            if (string.IsNullOrWhiteSpace(record.ResponseBody))
            {
                var http = new LogHttpRequestInfo()
                {
                    Route = httpContext.Request.Path,
                    Method = httpContext.Request.Method,
                    StatusCode = httpContext.Response.StatusCode,
                    ClientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unkown",
                };

                httpContext.Items.TryGetValue("auth", out var auth);

                _logger.LogWarning("Event:{@event},Http:{@http},Auth:{@auth},ErrorCode:{@errorcode},TraceId={@traceId},Msg:{@msg}, Key: {Key}", LogEvents.IdempotencyResponseDisappeared, http, auth, "Idempotency.responsebody.disappeared", Activity.Current?.Id ?? httpContext.TraceIdentifier, "Idempotency record succeeded but response body is empty.", context.Key);

                return IdempotencyResult.Processing();
            }

            var cachedResponse = JsonSerializer.Deserialize(
                record.ResponseBody,
                responseType);

            if (cachedResponse is null)
            {
                var http = new LogHttpRequestInfo()
                {
                    Route = httpContext.Request.Path,
                    Method = httpContext.Request.Method,
                    StatusCode = httpContext.Response.StatusCode,
                    ClientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unkown",
                };

                httpContext.Items.TryGetValue("auth", out var auth);

                _logger.LogWarning("Event:{@event},Http:{@http},Auth:{@auth},ErrorCode:{@errorcode},TraceId={@traceId},Msg:{@msg}, Key: {Key}", LogEvents.IdempotencyResponseDeserializeFail, http, auth, "Idempotency.responsebody.disappeared", Activity.Current?.Id ?? httpContext.TraceIdentifier, "Failed to deserialize cached idempotency response.", context.Key);

                return IdempotencyResult.Processing();
            }

            return IdempotencyResult.Cached(cachedResponse);
        }

        // 已经有请求正在处理。
        if (record.Status == IdempotencyRecordStatus.Processing)
        {
            return IdempotencyResult.Processing();
        }

        // 失败状态的处理策略要谨慎。
        //
        // 常见策略有两种：
        // 1. 不允许同一个 Key 重试，返回 Processing/Conflict，让客户端换 Key。
        // 2. 如果确认失败发生在业务执行前，允许重试。
        //
        // 这里使用保守策略：不自动重试，避免业务可能已部分成功时重复执行。
        if (record.Status == IdempotencyRecordStatus.Failed)
        {
            return IdempotencyResult.Processing();
        }

        return IdempotencyResult.Processing();
    }
}
