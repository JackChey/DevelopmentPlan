using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using Instructure.Attributes;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.UserCase.Products.Queries;

/// <summary>
/// 查询单个商品。
/// </summary>
[RequireAuthorization]
public sealed record GetProductByIdQuery(long Id)
    : IQuery<Result<ProductDto>>;

public sealed class GetProductByIdQueryValidator
    : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}

public sealed class GetProductByIdQueryHandler(
    IReadRepository<Product> productRepository)
    : IQueryHandler<GetProductByIdQuery, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.FirstOrDefaultAsNoTrackingAsync(
            product => product.Id == request.Id,
            cancellationToken);

        if (product is null || product.ProductStatus == AppProductStatus.Void)
        {
            return Result<ProductDto>.NotFound("商品不存在。");
        }

        return Result<ProductDto>.Success(ToDto(product));
    }

    private static ProductDto ToDto(Product product) => new(
        product.Id,
        product.ProductCode,
        product.ProductName,
        product.ProductDescription,
        product.ProductTypeId,
        product.ProductStatus,
        product.UnitPrice,
        product.Currency);
}
