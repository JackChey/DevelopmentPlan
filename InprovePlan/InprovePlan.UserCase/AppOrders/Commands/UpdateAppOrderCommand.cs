using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using InprovePlan.UserCase.Common.Attributes;
using Instructure.Caching;
using Instructure.Interfaces;
using Instructure.IResult;
using Instructure.Repositories;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InprovePlan.UserCase.AppOrders.Commands;

/// <summary>
/// 修改订单命令。
///
/// 主流业务中，订单支付后一般不允许修改商品、数量、地址。
/// 这里仅允许待支付订单 Addition 修改数量和地址。
/// </summary>
[RequireAuthorization]
public sealed record UpdateAppOrderCommand(
    long Id,
    decimal Quantity,
    long AddressId
) : ICommand<Result<AppOrderDto>>;

public sealed class UpdateAppOrderCommandValidator
    : AbstractValidator<UpdateAppOrderCommand>
{
    public UpdateAppOrderCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .PrecisionScale(18, 3, true);

        RuleFor(x => x.AddressId)
            .GreaterThan(0);
    }
}

public sealed class UpdateAppOrderCommandHandler(
    IRepository<AppOrder> orderRepository,
    IAppCache cache,
    ICacheKeyBuilder keyBuilder,
    IUser currentUser)
    : ICommandHandler<UpdateAppOrderCommand, Result<AppOrderDto>>
{
    public async Task<Result<AppOrderDto>> Handle(
        UpdateAppOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (order is null)
        {
            return Result<AppOrderDto>.NotFound("订单不存在。");
        }

        if (currentUser.Id is null || order.UserId != currentUser.Id.Value)
        {
            return Result<AppOrderDto>.Forbidden("无权修改该订单。");
        }

        if (order.OrderStatus != AppOrderStatus.Addition)
        {
            return Result<AppOrderDto>.Conflict("当前订单状态不允许修改。");
        }

        order.Quantity = Math.Round(request.Quantity, 3);
        order.AddressId = request.AddressId;
        order.RecalculateTotalAmount();

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
