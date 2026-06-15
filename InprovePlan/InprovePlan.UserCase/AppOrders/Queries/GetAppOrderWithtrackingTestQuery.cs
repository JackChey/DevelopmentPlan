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
public sealed record GetAppOrderWithtrackingTestQuery()
    : IQuery<Result<NPlusOneDemoResult>>;

public sealed class GetAppOrderWithtrackingTestQueryHandler(
    IReadRepository<AppOrder> orderRepository,
    QueryCounterInterceptor queryCounter)
    : IQueryHandler<GetAppOrderWithtrackingTestQuery, Result<NPlusOneDemoResult>>
{
    public async Task<Result<NPlusOneDemoResult>> Handle(GetAppOrderWithtrackingTestQuery request, CancellationToken cancellationToken)
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
                    EndTime: null))
            {  AsNoTracking = false},
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

        var items = new List<NPlusOneOrderItem>();

        foreach (var order in ordersResult.Items)
        {
            items.Add(new NPlusOneOrderItem(
                order.Id,
                order.OrderNo,
                string.Empty,
                order.ProductName,
                order.Quantity,
                order.UnitPrice * order.Quantity));
        }

        var snapshot = queryCounter.Snapshot();

        var result = new NPlusOneDemoResult(
            OrderCount: ordersResult.Items.Count,
            TotalSqlCount: snapshot.TotalCount,
            SelectSqlCount: snapshot.SelectCount,
            NonQuerySqlCount: snapshot.NonQueryCount,
            ExpectedNPlusOneDescription: $"当前订单数 {ordersResult.Items.Count}，预期会出现 {ordersResult.Items.Count} 次 Tacking。",
            SqlSamples: snapshot.Commands.Take(20).ToList(),
            Items: items);

        return Result<NPlusOneDemoResult>.Success(result);
    }
}


