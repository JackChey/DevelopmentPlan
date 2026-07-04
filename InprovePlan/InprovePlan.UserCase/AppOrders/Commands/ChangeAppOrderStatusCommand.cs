using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using Instructure.Attributes;
using Instructure.Caching;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.UserCase.AppOrders.Commands;

/// <summary>
/// 修改订单状态命令。
///
/// 适合后台或系统流程调用，
/// 例如支付完成、发货、运输、签收。
/// </summary>
[RequireAuthorization]
public sealed record ChangeAppOrderStatusCommand(
    long Id,
    AppOrderStatus OrderStatus
) : ICommand<Result<AppOrderDto>>;

public sealed class ChangeAppOrderStatusCommandValidator
    : AbstractValidator<ChangeAppOrderStatusCommand>
{
    public ChangeAppOrderStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.OrderStatus)
            .IsInEnum();
    }
}

public sealed class ChangeAppOrderStatusCommandHandler(
    IAppCache cache,
    ICacheKeyBuilder keyBuilder,
    IRepository<AppOrder> orderRepository)
    : ICommandHandler<ChangeAppOrderStatusCommand, Result<AppOrderDto>>
{
    public async Task<Result<AppOrderDto>> Handle(
        ChangeAppOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (order is null)
        {
            return Result<AppOrderDto>.NotFound("订单不存在。");
        }

        order.OrderStatus = request.OrderStatus;

        await orderRepository.SaveChangesAsync(cancellationToken);

        var cacheKey = keyBuilder.Build(
           module: "order",
           name: "detail",
           request.Id);

        await cache.RemoveAsync(
           cacheKey,
           cancellationToken);

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