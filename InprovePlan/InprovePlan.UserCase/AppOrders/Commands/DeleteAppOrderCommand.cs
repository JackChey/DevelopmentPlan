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
/// 删除订单命令。
///
/// 当前 AppOrder 没有 IsDeleted 或 Cancelled 状态。
/// 所以这里采用保守策略：
/// 仅允许删除待支付订单 Addition。
/// 已支付或后续状态订单不允许删除。
/// </summary>
[RequireAuthorization]
public sealed record DeleteAppOrderCommand(long Id)
    : ICommand<Result>;

public sealed class DeleteAppOrderCommandValidator
    : AbstractValidator<DeleteAppOrderCommand>
{
    public DeleteAppOrderCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}

public sealed class DeleteAppOrderCommandHandler(
    IRepository<AppOrder> orderRepository,
    IAppCache cache,
    ICacheKeyBuilder keyBuilder,
    IUser currentUser)
    : ICommandHandler<DeleteAppOrderCommand, Result>
{
    public async Task<Result> Handle(
        DeleteAppOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (order is null)
        {
            return Result.NotFound("订单不存在。");
        }

        if (currentUser.Id is null || order.UserId != currentUser.Id.Value)
        {
            return Result.Forbidden("无权删除该订单。");
        }

        if (order.OrderStatus != AppOrderStatus.Addition || order.Cancelled )
        {
            return Result.Conflict("当前订单状态不允许删除。");
        }

        orderRepository.Remove(order);

        await orderRepository.SaveChangesAsync(cancellationToken);

        var cacheKey = keyBuilder.Build(
           module: "order",
           name: "detail",
           request.Id);

        await cache.RemoveAsync(
           cacheKey,
           cancellationToken);

        return Result.SeccessWithNoMsg;
    }
}

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
