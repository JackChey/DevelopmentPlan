using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using InprovePlan.UserCase.Common.Attributes;
using Instructure.Interfaces;
using Instructure.IResult;
using Instructure.Repositories;

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
    IUser currentUser)
    : IQueryHandler<GetAppOrderByIdQuery, Result<AppOrderDto>>
{
    public async Task<Result<AppOrderDto>> Handle(
        GetAppOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.FirstOrDefaultAsNoTrackingAsync(
            order => order.Id == request.Id,
            cancellationToken);

        if (order is null)
        {
            return Result<AppOrderDto>.NotFound("订单不存在。");
        }

        if (currentUser.Id is null || order.UserId != currentUser.Id.Value)
        {
            return Result<AppOrderDto>.Forbidden("无权查看该订单。");
        }

        return Result<AppOrderDto>.Success(ToDto(order));
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