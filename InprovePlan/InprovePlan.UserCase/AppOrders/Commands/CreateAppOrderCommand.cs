using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using Instructure.Attributes;
using Instructure.Interfaces;
using Instructure.IResult;
using Instructure.Repositories;
using System.Security.Cryptography;

namespace InprovePlan.UserCase.AppOrders.Commands;

/// <summary>
/// 新增订单命令。
///
/// 业务场景：
/// 用户选择商品并下单。
/// 商品名称、编码、单价、币种会作为快照写入订单。
/// </summary>
[RequireAuthorization]
public sealed record CreateAppOrderCommand(
    long ProductId,
    decimal Quantity,
    long AddressId
) : ICommand<Result<AppOrderDto>>;

public sealed class CreateAppOrderCommandValidator
    : AbstractValidator<CreateAppOrderCommand>
{
    public CreateAppOrderCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0);

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .PrecisionScale(18, 3, true);

        RuleFor(x => x.AddressId)
            .GreaterThan(0);
    }
}

/// <summary>
/// 新增订单处理器。
///
/// 业务规则：
/// 1. 当前用户必须存在，由 AuthorizationBehavior 保证。
/// 2. 商品必须存在且状态为 Enable。
/// 3. 售罄、作废商品不能下单。
/// 4. 订单创建时保存商品快照。
/// 5. 新订单默认状态为 Addition。
/// </summary>
public sealed class CreateAppOrderCommandHandler(
    IRepository<AppOrder> orderRepository,
    IReadRepository<Product> productRepository,
    IUser currentUser)
    : ICommandHandler<CreateAppOrderCommand, Result<AppOrderDto>>
{
    public async Task<Result<AppOrderDto>> Handle(
        CreateAppOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is null)
        {
            return Result<AppOrderDto>.Unauthorized("未检测到当前用户。");
        }

        var product = await productRepository.FirstOrDefaultAsNoTrackingAsync(
            product => product.Id == request.ProductId,
            cancellationToken);

        if (product is null || product.ProductStatus == AppProductStatus.Void)
        {
            return Result<AppOrderDto>.NotFound("商品不存在。");
        }

        if (product.ProductStatus != AppProductStatus.Enable)
        {
            return Result<AppOrderDto>.Conflict("当前商品状态不允许下单。");
        }

        var order = new AppOrder
        {
            OrderNo = GenerateOrderNo(),
            ProductId = product.Id,
            ProductName = product.ProductName,
            ProductCode = product.ProductCode,
            Currency = product.Currency,
            UnitPrice = product.UnitPrice,
            Quantity = Math.Round(request.Quantity, 3),
            UserId = currentUser.Id.Value,
            OccurredTime = DateTimeOffset.UtcNow,
            OrderStatus = AppOrderStatus.Addition,
            Cancelled = false,
            AddressId = request.AddressId,
            Product = null!,
            User = null!
        };

        order.RecalculateTotalAmount();

        await orderRepository.AddAsync(order, cancellationToken);
        await orderRepository.SaveChangesAsync(cancellationToken);

        return Result<AppOrderDto>.Success(ToDto(order));
    }

    private static string GenerateOrderNo()
    {
        var random = RandomNumberGenerator.GetInt32(100000, 999999);
        return $"O{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}{random}";
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
