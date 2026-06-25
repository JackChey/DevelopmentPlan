using AutoMapper;
using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using Instructure.Interceptors;
using Instructure.IResult;
using Instructure.Paging;
using Instructure.Repositories;
using Instructure.Sorting;
using Instructure.Sorting.SortWhitelists;
using Instructure.Specification;
using System.Diagnostics;

namespace InprovePlan.UserCase.AppOrders.Queries;

/// <summary>
/// 查询订单及对应用户
/// 用于模拟慢SQL中无索引查询情况
/// </summary>
public sealed record GetAppOrderSlowSqlWithNoIndexTestQuery(long Id)
    : IQuery<Result<OrderTestQueryDemoResult>>;

public sealed class GetAppOrderSlowSqlWithNoIndexTestQueryValidator
    : AbstractValidator<GetAppOrderByIdQuery>
{
    public GetAppOrderSlowSqlWithNoIndexTestQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}

/// <summary>
/// 订单分页查询条件。
/// </summary>
public sealed class GetAppOrderSlowSqlWithNoIndexTestSpecification
    : Specification<AppOrder>
{
    public GetAppOrderSlowSqlWithNoIndexTestSpecification(GetAppOrderSlowSqlWithNoIndexTestQuery query)
    {
        AddCriteria(order => order.UserId == query.Id);
    }
}

public sealed class GetAppOrderSlowSqlWithNoIndexTestQueryHandler(
    IReadRepository<AppOrder> orderRepository,
    QueryCounterInterceptor queryCounter)
    : IQueryHandler<GetAppOrderSlowSqlWithNoIndexTestQuery, Result<OrderTestQueryDemoResult>>
{
    public async Task<Result<OrderTestQueryDemoResult>> Handle(GetAppOrderSlowSqlWithNoIndexTestQuery request, CancellationToken cancellationToken)
    {
        queryCounter.Reset();

        // 模拟订单中有 10000 条数据,
        // 模拟情景:先将本次查询条件:用户Id 从订单表中删除索引,再将索引加回来,对比二者查询效率

        Stopwatch stopwatch = Stopwatch.StartNew();

        var queryResult = await orderRepository.ListAsync(new GetAppOrderSlowSqlWithNoIndexTestSpecification(request));

        var snapshot = queryCounter.Snapshot();

        stopwatch.Stop();

        var orderResult = new List<OrderTestQueryItem>();

        foreach (var order in queryResult)
        {
            orderResult.Add(new OrderTestQueryItem(
                order.Id,
                order.OrderNo,
                string.Empty,
                order.ProductName,
                order.Quantity,
                order.UnitPrice * order.Quantity));
        }

        var result = new OrderTestQueryDemoResult(
            OrderCount: orderResult.Count,
            TotalSqlCount: snapshot.TotalCount,
            SelectSqlCount: snapshot.SelectCount,
            NonQuerySqlCount: snapshot.NonQueryCount,
            ExpectedNPlusOneDescription: $"本次查询用时:{stopwatch.Elapsed.TotalMilliseconds} ms",
            SqlSamples: snapshot.Commands.Take(20).ToList(),
            Items: orderResult);

        return Result<OrderTestQueryDemoResult>.Success(result);
    }
}


