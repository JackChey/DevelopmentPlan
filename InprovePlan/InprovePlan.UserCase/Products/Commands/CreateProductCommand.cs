using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using InprovePlan.UserCase.Common.Attributes;
using Instructure.Configurations.Entities;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.UserCase.Products.Commands;

/// <summary>
/// 新增商品命令。
/// </summary>
[RequireAuthorization]
public sealed record CreateProductCommand(
    string ProductCode,
    string ProductName,
    string ProductDescription,
    int ProductTypeId,
    decimal UnitPrice,
    string Currency
) : ICommand<Result<ProductDto>>;

/// <summary>
/// 新增商品参数校验。
/// </summary>
public sealed class CreateProductCommandValidator
    : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.ProductCode)
            .NotEmpty()
            .MaximumLength(DataSchemaConstants.ProductCodeLength);

        RuleFor(x => x.ProductName)
            .NotEmpty()
            .MaximumLength(DataSchemaConstants.ProductNameLength);

        RuleFor(x => x.ProductDescription)
            .NotEmpty()
            .MaximumLength(DataSchemaConstants.ProductDescriptionLength);

        RuleFor(x => x.ProductTypeId)
            .GreaterThan(0);

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(DataSchemaConstants.CurrencyLength);
    }
}

/// <summary>
/// 新增商品处理器。
///
/// 业务规则：
/// 1. 商品编码必须唯一。
/// 2. 新增商品默认启用。
/// 3. Id、CreatedAt、CreatedByUserId 由审计拦截器处理。
/// </summary>
public sealed class CreateProductCommandHandler(
    IRepository<Product> productRepository)
    : ICommandHandler<CreateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var productCode = request.ProductCode.Trim().ToUpperInvariant();

        var codeExists = await productRepository.AnyAsync(
            product => product.ProductCode == productCode,
            cancellationToken);

        if (codeExists)
        {
            return Result<ProductDto>.Conflict("商品编码已存在。");
        }

        var product = new Product
        {
            ProductCode = productCode,
            ProductName = request.ProductName.Trim(),
            ProductDescription = request.ProductDescription.Trim(),
            ProductTypeId = request.ProductTypeId,
            ProductStatus = AppProductStatus.Enable,
            UnitPrice = Math.Round(request.UnitPrice, 2),
            Currency = request.Currency.Trim().ToUpperInvariant()
        };

        await productRepository.AddAsync(product, cancellationToken);
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