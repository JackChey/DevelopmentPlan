using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using InprovePlan.UserCase.Common.Attributes;
using Instructure.Caching;
using Instructure.Interfaces;
using Instructure.IResult;
using Instructure.Repositories;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackExchange.Redis;

namespace InprovePlan.UserCase.AppOrders.Queries;

/// <summary>
/// 查询单个订单。
/// </summary>
[RequireAuthorization]
public sealed record GetAppOrderByIdQuery(long Id)
    : IQuery<Result<AppOrderDto>>;

public sealed class GetAppOrderByIdQueryValidator
    : AbstractValidator<GetAppOrderByIdQuery>
{
    public GetAppOrderByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}

public sealed class GetAppOrderByIdQueryHandler(
    IReadRepository<AppOrder> orderRepository,
    IAppCache cache,
    ICacheKeyBuilder keyBuilder,
    IUser currentUser)
    : IQueryHandler<GetAppOrderByIdQuery, Result<AppOrderDto>>
{
    public async Task<Result<AppOrderDto>> Handle(
        GetAppOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        // 先判断当前用户。
        // 不建议把未登录、无权限这类结果放进缓存，
        // 因为这些状态和当前请求上下文有关。
        if (currentUser.Id is null)
        {
            return Result<AppOrderDto>.Unauthorized("用户未登录。");
        }

        // 构造缓存 Key。
        // 这里按订单 Id 缓存订单详情数据。
        // 注意：这个 Key 是订单维度，不是用户维度，
        // 所以后面仍然必须检查 orderDto.UserId 是否属于当前用户。
        var cacheKey = keyBuilder.Build(
            module: "order",
            name: "detail",
            request.Id);

        var orderDto = await cache.GetOrSetAsync<AppOrderDto>(
            cacheKey,
            async ct =>
            {
                // 缓存未命中时才会执行这里的数据库查询。
                //
                // 生产中建议缓存 DTO，不缓存 EF Core Entity。
                // 如果 repository 支持 IQueryable 投影，优先直接 Select 成 DTO，
                // 这样可以减少查询列和 ChangeTracker 成本。
                var order = await orderRepository.FirstOrDefaultAsNoTrackingAsync(
                    order => order.Id == request.Id,
                    ct);

                if (order is null)
                {
                    // 返回 null 后，AppCache 可以缓存一个短 TTL 的空值，
                    // 用来防止不存在的订单 Id 反复打到数据库。
                    return null;
                }

                return ToDto(order);
            },
            new CacheEntryPolicy
            {
                // 订单详情属于会变化的数据，TTL 不宜过长。
                Duration = TimeSpan.FromMinutes(3),

                // 不存在的订单短时间缓存即可。
                // 避免刚创建或恢复的数据因为空值缓存导致长时间查不到。
                NullValueDuration = TimeSpan.FromSeconds(30),

                // 开启空值缓存，防止缓存穿透。
                CacheNullValue = true
            },
            cancellationToken);

        if (orderDto is null)
        {
            return Result<AppOrderDto>.NotFound("订单不存在。");
        }

        // 即使命中缓存，也必须做权限判断。
        // 因为缓存 Key 是 order:detail:{orderId}，不同用户可能访问同一个 Key。
        if (orderDto.UserId != currentUser.Id.Value)
        {
            return Result<AppOrderDto>.Forbidden("无权查看该订单。");
        }

        return Result<AppOrderDto>.Success(orderDto);
    }

    private static AppOrderDto ToDto(AppOrder order) => new(
        order.Id,
        order.OrderNo,
        order.ProductId,
        order.ProductName,
        order.ProductCode,
        order.Currency,
        order.UnitPrice,
        order.Quantity,
        order.UnitPrice * order.Quantity,
        order.UserId,
        order.OccurredTime,
        order.OrderStatus,
        order.Cancelled,
        order.AddressId);
}