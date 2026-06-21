using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using Instructure.Interceptors;
using Instructure.IResult;
using Instructure.Paging;
using Instructure.Repositories;
using Instructure.Sorting;
using Instructure.Sorting.SortWhitelists;

namespace InprovePlan.UserCase.AppOrders.Queries;

/// <summary>
/// 查询订单及对应用户
/// </summary>
public sealed record GetAppOrderWithNotrackingTestQuery()
    : IQuery<Result<OrderTestQueryDemoResult>>;

public sealed class GetAppOrderWithNotrackingTestQueryHandler(
    IReadRepository<AppOrder> orderRepository,
    QueryCounterInterceptor queryCounter)
    : IQueryHandler<GetAppOrderWithNotrackingTestQuery, Result<OrderTestQueryDemoResult>>
{
    public async Task<Result<OrderTestQueryDemoResult>> Handle(GetAppOrderWithNotrackingTestQuery request, CancellationToken cancellationToken)
    {
        queryCounter.Reset();

        int pageSize = 50;

        var ordersResult = await orderRepository.PageAsync(
            new AppOrdersPagedSpecification(
                new GetAppOrdersPagedQuery(
                    new Pagination
                    {
                        PageIndex = 1,
                        PageSize = pageSize
                    },
                    new SortQuery
                    {
                        SortBy = "createdAt",
                        SortDirection = "desc"
                    },
                    Keyword: null,
                    UserId: null,
                    ProductId: null,
                    OrderStatus: null,
                    StartTime: null,
                    EndTime: null)),
            new Pagination
            {
                PageIndex = 1,
                PageSize = pageSize
            },
            new SortQuery
            {
                SortBy = "createdAt",
                SortDirection = "desc"
            },
            AppOrderSortWhitelist.Instance,
            cancellationToken);

        var items = new List<OrderTestQueryItem>();

        foreach (var order in ordersResult.Items)
        {
            items.Add(new OrderTestQueryItem(
                order.Id,
                order.OrderNo,
                string.Empty,
                order.ProductName,
                order.Quantity,
                order.UnitPrice * order.Quantity));
        }

        var snapshot = queryCounter.Snapshot();

        var result = new OrderTestQueryDemoResult(
            OrderCount: ordersResult.Items.Count,
            TotalSqlCount: snapshot.TotalCount,
            SelectSqlCount: snapshot.SelectCount,
            NonQuerySqlCount: snapshot.NonQueryCount,
            ExpectedNPlusOneDescription: $"当前订单数 {ordersResult.Items.Count}，预期会出现约 0 次 Tracking。",
            SqlSamples: snapshot.Commands.Take(20).ToList(),
            Items: items);

        return Result<OrderTestQueryDemoResult>.Success(result);
    }
}


