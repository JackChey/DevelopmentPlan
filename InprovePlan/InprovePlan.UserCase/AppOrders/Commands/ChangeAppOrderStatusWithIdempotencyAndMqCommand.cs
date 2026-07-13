using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Contracts;
using InprovePlan.ShareKernel.Messaging;
using Instructure.Attributes;
using Instructure.Caching;
using Instructure.Interfaces;
using Instructure.IResult;
using Instructure.Repositories;
using StackExchange.Redis;

namespace InprovePlan.UserCase.AppOrders.Commands;

/// <summary>
/// 修改订单状态命令。
///
/// 适合后台或系统流程调用，
/// 例如支付完成、发货、运输、签收。
/// </summary>
[RequireAuthorization]
public sealed record ChangeAppOrderStatusWithIdempotencyAndMqCommand(
    long Id,
    AppOrderStatus OrderStatus,
    string UpdateReason,
    string IdempotencyKey
) : ICommand<Result<AppOrderDto>>, IIdempotentRequest;

public sealed class ChangeAppOrderStatusWithIdempotencyAndMqCommandValidator
    : AbstractValidator<ChangeAppOrderStatusWithIdempotencyAndMqCommand>
{
    public ChangeAppOrderStatusWithIdempotencyAndMqCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.OrderStatus)
            .IsInEnum();

        RuleFor(x => x.IdempotencyKey)
            .NotNull()
            .NotEmpty();

        RuleFor(x => x.UpdateReason)
            .NotNull()
            .NotEmpty();
    }
}

public sealed class ChangeAppOrderStatusWithIdempotencyAndMqCommandHandler(
    IAppCache cache,
    ICacheKeyBuilder keyBuilder,
    IUser user,
    IOrderEventPublisher eventPublisher,
    IRepository<AppOrder> orderRepository)
    : ICommandHandler<ChangeAppOrderStatusWithIdempotencyAndMqCommand, Result<AppOrderDto>>
{
    public async Task<Result<AppOrderDto>> Handle(
        ChangeAppOrderStatusWithIdempotencyAndMqCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (order is null)
        {
            return Result<AppOrderDto>.NotFound("订单不存在。");
        }


        // TODO: 这里是你的订单状态修改逻辑。
        // await _orderRepository.UpdateStatusAsync(...);

        var @event = new OrderStatusChangedEvent
        {
            MessageId = Guid.NewGuid(),
            OrderId = order.Id,
            FromStatus = request.OrderStatus,
            ToStatus = order.OrderStatus,
            Reason = request.UpdateReason,
            OperatorId = user.Id,
            OccurredAt = DateTimeOffset.UtcNow,
            TraceId = null
        };

        // 这里看起来像是“发布 MQ”，
        // 但启用 EF Outbox 后，本质上是“写 Outbox 表”。
        // 当前事务提交后，MassTransit 后台服务才会把消息发送到 RabbitMQ。
        await eventPublisher.PublishOrderStatusChangedAsync(@event, cancellationToken);

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