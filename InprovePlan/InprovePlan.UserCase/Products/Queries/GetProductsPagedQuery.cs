using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using InprovePlan.UserCase.Common.Attributes;
using Instructure.Configurations.Entities;
using Instructure.IResult;
using Instructure.Paging;
using Instructure.Repositories;
using Instructure.Sorting.SortWhitelists;
using Instructure.Sorting;
using Instructure.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InprovePlan.UserCase.Products.Queries;

/// <summary>
/// 商品分页查询。
/// </summary>
[RequireAuthorization]
public sealed record GetProductsPagedQuery(
    Pagination Pagination,
    SortQuery Sort,
    string? Keyword,
    int? ProductTypeId,
    AppProductStatus? ProductStatus,
    bool IncludeVoid = false
) : IQuery<Result<PagedResult<ProductDto>>>;

public sealed class GetProductsPagedQueryValidator
    : AbstractValidator<GetProductsPagedQuery>
{
    public GetProductsPagedQueryValidator()
    {
        RuleFor(x => x.Pagination)
            .NotNull();

        RuleFor(x => x.Pagination.PageIndex)
            .GreaterThanOrEqualTo(1)
            .When(x => x.Pagination is not null);

        RuleFor(x => x.Pagination.PageSize)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(Pagination.MaxPageSize)
            .When(x => x.Pagination is not null);

        RuleFor(x => x.Sort)
            .NotNull();

        RuleFor(x => x.Keyword)
            .MaximumLength(DataSchemaConstants.ProductNameLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Keyword));

        RuleFor(x => x.ProductTypeId)
            .GreaterThan(0)
            .When(x => x.ProductTypeId.HasValue);

        RuleFor(x => x.ProductStatus)
            .IsInEnum()
            .When(x => x.ProductStatus.HasValue);
    }
}

/// <summary>
/// 商品分页查询条件。
/// </summary>
public sealed class ProductsPagedSpecification
    : Specification<Product>
{
    public ProductsPagedSpecification(GetProductsPagedQuery query)
    {
        if (!query.IncludeVoid)
        {
            AddCriteria(product => product.ProductStatus != AppProductStatus.Void);
        }

        if (query.ProductTypeId.HasValue)
        {
            AddCriteria(product => product.ProductTypeId == query.ProductTypeId.Value);
        }

        if (query.ProductStatus.HasValue)
        {
            AddCriteria(product => product.ProductStatus == query.ProductStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();

            AddCriteria(product =>
                product.ProductName.Contains(keyword)
                || product.ProductCode.Contains(keyword));
        }
    }
}

public sealed class GetProductsPagedQueryHandler(
    IReadRepository<Product> productRepository)
    : IQueryHandler<GetProductsPagedQuery, Result<PagedResult<ProductDto>>>
{
    public async Task<Result<PagedResult<ProductDto>>> Handle(
        GetProductsPagedQuery request,
        CancellationToken cancellationToken)
    {
        var sortErrors = ProductSortWhitelist.Instance.Validate(request.Sort);

        if (sortErrors.Count > 0)
        {
            return Result<PagedResult<ProductDto>>.Invalid(
                sortErrors.Select(x => x.Message).ToArray());
        }

        var products = await productRepository.PageAsync(
            new ProductsPagedSpecification(request),
            request.Pagination,
            request.Sort,
            ProductSortWhitelist.Instance,
            cancellationToken);

        var dtoItems = products.Items
            .Select(product => new ProductDto(
                product.Id,
                product.ProductCode,
                product.ProductName,
                product.ProductDescription,
                product.ProductTypeId,
                product.ProductStatus,
                product.UnitPrice,
                product.Currency))
            .ToList();

        return Result<PagedResult<ProductDto>>.Success(
            PagedResult<ProductDto>.Create(dtoItems, products.Total, request.Pagination));
    }
}
