using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using Instructure.Attributes;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.UserCase.Products.Commands;

/// <summary>
/// 删除商品命令。
///
/// 当前 Product 没有 IsDeleted 字段。
/// 因此这里采用业务作废，而不是物理删除。
/// 这样可以避免历史订单外键受影响。
/// </summary>
[RequireAuthorization]
public sealed record DeleteProductCommand(long Id)
    : ICommand<Result>;

public sealed class DeleteProductCommandValidator
    : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}

public sealed class DeleteProductCommandHandler(
    IRepository<Product> productRepository)
    : ICommandHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(
        DeleteProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (product is null || product.ProductStatus == AppProductStatus.Void)
        {
            return Result.NotFound("商品不存在。");
        }

        product.ProductStatus = AppProductStatus.Void;

        await productRepository.SaveChangesAsync(cancellationToken);

        return Result.SeccessWithNoMsg;
    }
}