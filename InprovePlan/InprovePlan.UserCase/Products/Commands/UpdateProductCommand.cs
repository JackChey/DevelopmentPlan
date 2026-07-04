using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using Instructure.Attributes;
using Instructure.Configurations.Entities;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.UserCase.Products.Commands;

/// <summary>
/// 修改商品命令。
/// </summary>
[RequireAuthorization]
public sealed record UpdateProductCommand(
    long Id,
    string ProductName,
    string ProductDescription,
    long ProductTypeId,
    AppProductStatus ProductStatus,
    decimal UnitPrice,
    string Currency
) : ICommand<Result<ProductDto>>;

public sealed class UpdateProductCommandValidator
    : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.ProductName)
            .NotEmpty()
            .MaximumLength(DataSchemaConstants.ProductNameLength);

        RuleFor(x => x.ProductDescription)
            .NotEmpty()
            .MaximumLength(DataSchemaConstants.ProductDescriptionLength);

        RuleFor(x => x.ProductTypeId)
            .GreaterThan(0);

        RuleFor(x => x.ProductStatus)
            .IsInEnum();

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(DataSchemaConstants.CurrencyLength);
    }
}

/// <summary>
/// 修改商品处理器。
///
/// 注意：
/// 商品编码通常作为业务唯一编码，不建议在普通修改接口中修改。
/// 如果确实需要修改，应单独设计变更商品编码命令。
/// </summary>
public sealed class UpdateProductCommandHandler(
    IRepository<Product> productRepository)
    : ICommandHandler<UpdateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (product is null || product.ProductStatus == AppProductStatus.Void)
        {
            return Result<ProductDto>.NotFound("商品不存在。");
        }

        product.ProductName = request.ProductName.Trim();
        product.ProductDescription = request.ProductDescription.Trim();
        product.ProductTypeId = request.ProductTypeId;
        product.ProductStatus = request.ProductStatus;
        product.UnitPrice = Math.Round(request.UnitPrice, 2);
        product.Currency = request.Currency.Trim().ToUpperInvariant();

        await productRepository.SaveChangesAsync(cancellationToken);

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