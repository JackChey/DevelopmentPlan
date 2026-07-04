using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using Instructure.Attributes;
using Instructure.Configurations.Entities;
using Instructure.IResult;
using Instructure.Paging;
using Instructure.Repositories;
using Instructure.Sorting;
using Instructure.Sorting.SortWhitelists;
using Instructure.Specification;

namespace InprovePlan.UserCase.AppOrders.Queries;

/// <summary>
/// 订单分页查询。
/// </summary>
[RequireAuthorization]
public sealed record GetAppOrdersPagedQuery(
    Pagination Pagination,
    SortQuery Sort,
    string? Keyword,
    long? UserId,
    long? ProductId,
    AppOrderStatus? OrderStatus,
    DateTimeOffset? StartTime,
    DateTimeOffset? EndTime
) : IQuery<Result<PagedResult<AppOrderDto>>>;

public sealed class GetAppOrdersPagedQueryValidator
    : AbstractValidator<GetAppOrdersPagedQuery>
{
    public GetAppOrdersPagedQueryValidator()
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
            .MaximumLength(DataSchemaConstants.OrderNoLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Keyword));

        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .When(x => x.UserId.HasValue);

        RuleFor(x => x.ProductId)
            .GreaterThan(0)
            .When(x => x.ProductId.HasValue);

        RuleFor(x => x.OrderStatus)
            .IsInEnum()
            .When(x => x.OrderStatus.HasValue);

        RuleFor(x => x)
            .Must(x => !x.StartTime.HasValue
                       || !x.EndTime.HasValue
                       || x.StartTime.Value < x.EndTime.Value)
            .WithMessage("开始时间必须小于结束时间。");
    }
}

/// <summary>
/// 订单分页查询条件。
/// </summary>
public sealed class AppOrdersPagedSpecification
    : Specification<AppOrder>
{
    public AppOrdersPagedSpecification(GetAppOrdersPagedQuery query)
    {
        if (query.UserId.HasValue)
        {
            AddCriteria(order => order.UserId == query.UserId.Value);
        }

        if (query.ProductId.HasValue)
        {
            AddCriteria(order => order.ProductId == query.ProductId.Value);
        }

        if (query.OrderStatus.HasValue)
        {
            AddCriteria(order => order.OrderStatus == query.OrderStatus.Value);
        }

        if (query.StartTime.HasValue)
        {
            AddCriteria(order => order.OccurredTime >= query.StartTime.Value);
        }

        if (query.EndTime.HasValue)
        {
            AddCriteria(order => order.OccurredTime < query.EndTime.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();

            AddCriteria(order =>
                order.OrderNo.Contains(keyword)
                || order.ProductName.Contains(keyword)
                || order.ProductCode.Contains(keyword));
        }
    }
}

public sealed class GetAppOrdersPagedQueryHandler(
    IReadRepository<AppOrder> orderRepository)
    : IQueryHandler<GetAppOrdersPagedQuery, Result<PagedResult<AppOrderDto>>>
{
    public async Task<Result<PagedResult<AppOrderDto>>> Handle(
        GetAppOrdersPagedQuery request,
        CancellationToken cancellationToken)
    {
        var sortErrors = AppOrderSortWhitelist.Instance.Validate(request.Sort);

        if (sortErrors.Count > 0)
        {
            return Result<PagedResult<AppOrderDto>>.Invalid(
                sortErrors.Select(x => x.Message).ToArray());
        }

        var orders = await orderRepository.PageAsync(
            new AppOrdersPagedSpecification(request),
            request.Pagination,
            request.Sort,
            AppOrderSortWhitelist.Instance,
            cancellationToken);

        var dtoItems = orders.Items
            .Select(order => new AppOrderDto(
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
                order.AddressId))
            .ToList();

        return Result<PagedResult<AppOrderDto>>.Success(
            PagedResult<AppOrderDto>.Create(dtoItems, orders.Total, request.Pagination));
    }
}