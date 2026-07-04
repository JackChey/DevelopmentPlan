using Instructure.Exceptions;
using Instructure.Idempotency;
using Instructure.Interfaces;
using Instructure.IResult;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InprovePlan.UserCase.Behaviors;

/// <summary>
/// MediatR 幂等管道行为。
/// 
/// 该 Behavior 负责对实现了 IIdempotentRequest 的业务请求执行幂等控制。
/// 
/// 它的定位是“业务操作执行管道”，而不是 HTTP Filter。
/// 
/// 推荐流程：
/// 1. Controller 从 Header 读取 Idempotency-Key。
/// 2. Controller 构造 Command，并把 IdempotencyKey/UserId/TenantId 赋值给 Command。
/// 3. Controller 调用 _mediator.Send(command)。
/// 4. IdempotencyBehavior 拦截该 Command。
/// 5. Behavior 根据幂等结果决定是否继续执行 Handler。
/// 
/// 这样做的好处：
/// 1. 幂等保护的是业务命令，而不是某个 Controller Action。
/// 2. 同一个 Command 被 HTTP、消息、后台任务调用时，可以复用同一套幂等逻辑。
/// 3. Controller 保持轻量，只做协议层适配。
/// 4. Handler 保持纯粹，只处理业务逻辑。
/// </summary>
/// <typeparam name="TRequest">
/// MediatR 请求类型。
/// </typeparam>
/// <typeparam name="TResponse">
/// MediatR 响应类型。
/// 
/// 这里约束为 IResult，是因为幂等命中缓存、处理中、冲突时，
/// Behavior 需要直接构造统一响应结果返回。
/// </typeparam>
public sealed class IdempotencyBehavior<TRequest, TResponse>(
    IUser currentUser,
    ILogger<IdempotencyBehavior<TRequest, TResponse>> logger,
    IHttpContextAccessor httpContextAccessor,
    IRequestHashProvider _requestHashProvider,
    IIdempotencyService _idempotencyService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // --- 第一步：检查是否启用了幂等性 ---

        // 检查当前 Action 或 Controller 是否声明了 [Idempotent] 特性。
        // 只有显式标记的接口才会经过此过滤器的幂等逻辑，避免对普通 GET/POST 请求造成性能损耗。
        // 如果没有声明幂等特性，直接放行，不执行任何额外逻辑。
        if (request is not IIdempotentRequest idempotentRequest)
        {
            return await next();
        }

        var httpContext = httpContextAccessor.HttpContext;

        // --- 第二步：提取并验证幂等键 (Idempotency-Key) ---

        // 验证幂等键不能为空或空白字符串。
        if (string.IsNullOrWhiteSpace(idempotentRequest.IdempotencyKey))
        {
            // 如果缺少必要的 Header，返回 400 Bad Request。
            ThrowIdempotencyFailure(logger, IdempotencyFailureStatus.BadRequest, "idempotency_key_required", "Idempotency-Key is required");
        }

        // --- 第三步：构建用户身份与请求上下文 ---

        // 计算请求 Hash。
        //
        // 作用：
        // 防止同一个 Idempotency-Key 被复用到不同请求内容上。
        //
        // 注意：
        // 这里序列化的是 MediatR Request，也就是业务命令对象。
        // 因此它不依赖 HTTP Request.Body，未来消息消费、后台任务也能复用。
        //
        // 建议 RequestHashProvider 内部做稳定序列化：
        // 1. 属性顺序稳定
        // 2. 忽略不参与幂等判断的字段
        // 3. 日期、decimal 等格式稳定
        var requestHash = ComputeRequestHash(request, idempotentRequest);

        var context = new IdempotencyRequestContext
        {
            Key = idempotentRequest.IdempotencyKey,
            RequestHash = requestHash,
            UserId = currentUser.Id!.Value,

            // 已经不依赖 HTTP 后，Method 和 Path 可以换成业务语义。
            // 这里建议用 Request 类型名作为 Path/OperationName。
            //
            // 这样数据库中看到的记录会是：
            // Method = "MediatR_Check_Idempotency"
            // Path = "CreateOrderCommand"
            Method = "MediatR_Check_Idempotency",
            Path = typeof(TRequest).FullName ?? typeof(TRequest).Name
        };

        // --- 第四步：执行幂等性检查 (Begin) ---

        // 调用服务层的 BeginAsync 方法。
        // 该方法会尝试创建记录或查询现有记录，并返回当前请求的状态。
        // BeginAsync 是幂等判断的核心。
        //
        // 它通常会做：
        // 1. 获取分布式锁
        // 2. 尝试插入 Processing 记录
        // 3. 读取已有幂等记录
        // 4. 判断 Hash 是否一致
        // 5. 判断是否命中缓存
        var beginResult = await _idempotencyService.BeginAsync(
            context,
            typeof(TResponse),
            cancellationToken);


        // 情况 A：命中缓存 (Succeeded)
        // 之前已经成功处理过相同的请求，直接返回缓存的状态码和响应体，不再执行业务逻辑。
        if (beginResult.State == IdempotencyResultState.Cached)
        {
             if (beginResult.CachedResponse is TResponse cachedResponse)
            {
                return cachedResponse;
            }

            throw new IdempotencyException(IdempotencyFailureStatus.NotFound, "request_notfound", "Cached idempotency response type mismatch.");
        }

        // 情况 B：正在处理中 (Processing)
        // 另一个相同的请求正在被执行中，返回 409 Conflict，提示客户端稍后重试。
        if (beginResult.State == IdempotencyResultState.Processing)
        {
            throw new IdempotencyException(IdempotencyFailureStatus.Conflict‌, "request_conflict", "The same idempotent request is still processing.");
        }

        // 情况 C：冲突 (Conflict)
        // 相同的 Key 被用于不同的请求内容（Hash 不匹配），这是潜在的错误或攻击，必须拒绝。
        if (beginResult.State == IdempotencyResultState.Conflict)
        {
            throw new IdempotencyException(IdempotencyFailureStatus.BadRequest, "request_conflict", "The same idempotency key was used with a different request payload.");
        }

        // 如果不是 Started，说明出现了未知状态。
        // 这里保守处理为失败，避免请求绕过幂等保护继续执行业务。
        if (beginResult.State != IdempotencyResultState.Started)
        {
            throw new IdempotencyException(IdempotencyFailureStatus.NotFound, "request_notfound", "Unexpected idempotency state.");
        }

        // 情况 D：首次请求 (Started)
        // 通过了所有检查，允许继续执行后续的业务逻辑。

        try
        {
            // 首次请求，执行后续管道和真正的 Handler。
            var response = await next();

            // 如果业务执行成功，则保存完整统一响应结果。
            //
            // 重复请求命中缓存时，可以直接反序列化并返回同样的 IResult。
            //
            // 是否只缓存成功结果：
            // 一般建议只缓存成功结果。
            // 对失败结果是否缓存，要看业务。
            // 例如参数校验失败可以不缓存，外部支付已提交但响应失败则需要谨慎处理。
            if (response.IsSuccess)
            {
                await _idempotencyService.CompleteAsync(
                    context,
                    response,
                    typeof(TResponse),
                    cancellationToken);
            }
            else
            {
                // 业务失败时是否标记 Failed，需要按业务策略决定。
                //
                // 这里给出保守做法：
                // 记录失败状态，但不把它当作成功缓存。
                // 后续相同 Key 再来时，可以根据 Failed 状态决定是否允许重试。
                await _idempotencyService.FailAsync(
                    context,
                    response.Message ?? "Business request failed.",
                    cancellationToken);
            }

            return response;
        }
        catch (Exception ex)
        {
            await _idempotencyService.FailAsync(
                context,
                ex,
                cancellationToken);

            throw;
        }
    }

    /// <summary>
    /// 记录幂等失败日志，并抛出异常。
    /// 
    /// 授权失败通常不是系统故障，
    /// 所以使用 Warning，而不是 Error。
    /// </summary>
    private void ThrowIdempotencyFailure(
        ILogger logger,
        IdempotencyFailureStatus status,
        string code,
        string message)
    {
        logger.LogWarning("Idempotency Failed,event:{@event}, errorcode:{@errorcode}, msg:{@msg}", "idempotency_failed", code, message);

        throw new IdempotencyException(
            status,
            code,
            message);
    }

    /// <summary>
    /// 计算当前 MediatR 请求的请求 Hash。
    /// 
    /// 这里不使用 HTTP Method / Path / Body，
    /// 而是使用业务 Command 的类型和值。
    /// </summary>
    private string ComputeRequestHash(
        TRequest request,
        IIdempotentRequest idempotentRequest)
    {
        // 注意：
        // IdempotencyKey 本身通常不应该参与 Hash。
        //
        // 因为 Hash 的目的，是判断“同一个 Key 对应的业务参数是否一致”。
        // 如果把 Key 放进去，虽然通常也不会错，但语义上没必要。
        //
        // 更严格的实现可以在 RequestHashProvider 中忽略 IdempotencyKey 字段。
        var body = JsonSerializer.Serialize(request, request.GetType());

        return _requestHashProvider.ComputeHash(new RequestHashSource
        {
            Method = "MediatR_ComputeRequestHash",
            Path = typeof(TRequest).FullName ?? typeof(TRequest).Name,
            QueryString = string.Empty,
            UserId = currentUser.Id ?? 0,
            Body = body
        });
    }
}





