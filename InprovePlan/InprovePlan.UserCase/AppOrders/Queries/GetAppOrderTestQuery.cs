using FluentValidation;
using InprovePlan.Domain.BaseEntities;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using InprovePlan.UserCase.AppUsers.Queries;
using InprovePlan.UserCase.Common.Attributes;
using Instructure.Interceptors;
using Instructure.Interfaces;
using Instructure.IResult;
using Instructure.OffsetLimiting;
using Instructure.Paging;
using Instructure.Repositories;
using Instructure.Sorting;
using Instructure.Sorting.SortWhitelists;
using Instructure.Specification;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace InprovePlan.UserCase.AppOrders.Queries;

/// <summary>
/// 查询订单及对应用户
/// </summary>
public sealed record GetAppOrderTestQuery()
    : IQuery<Result<NPlusOneDemoResult>>;

public sealed class GetAppOrderTestQueryHandler(
    IReadRepository<AppOrder> orderRepository,
    IReadRepository<AppUser> userRepository,
    IReadRepository<Product> productRepository,
    QueryCounterInterceptor queryCounter)
    : IQueryHandler<GetAppOrderTestQuery, Result<NPlusOneDemoResult>>
{
    public async Task<Result<NPlusOneDemoResult>> Handle(GetAppOrderTestQuery request, CancellationToken cancellationToken)
    {
        queryCounter.Reset();

        int pageSize = 20;

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

        var items = new List<NPlusOneOrderItem>();

        // 这里是不触发 n+1 的写法
        var userIds = ordersResult.Items
           .Select(order => order.UserId)
           .Distinct()
           .ToArray();

        var productIds = ordersResult.Items
            .Select(order => order.ProductId)
            .Distinct()
            .ToArray();

        var users = await userRepository.ListAsync(
            new AppUsersByIdsSpecification(userIds),
            cancellationToken);

        var products = await productRepository.ListAsync(
            new ProductsByIdsSpecification(productIds),
            cancellationToken);

        foreach (var order in ordersResult.Items)
        {
            // N 次用户查询。
            var user = users.FirstOrDefault(
                user => user.Id == order.UserId);

            // N 次商品查询。
            var product = products.FirstOrDefault(
                product => product.Id == order.ProductId);

            items.Add(new NPlusOneOrderItem(
                order.Id,
                order.OrderNo,
                user?.UserName ?? string.Empty,
                product?.ProductName ?? order.ProductName,
                order.Quantity,
                order.UnitPrice * order.Quantity));
        }

        // 这是之前的触发 n+1 的写法
        //foreach (var order in ordersResult.Items)
        //{
        //    // N 次用户查询。
        //    var user = await userRepository.FirstOrDefaultAsNoTrackingAsync(
        //        user => user.Id == order.UserId,
        //        cancellationToken);

        //    // N 次商品查询。
        //    var product = await productRepository.FirstOrDefaultAsNoTrackingAsync(
        //        product => product.Id == order.ProductId,
        //        cancellationToken);

        //    items.Add(new NPlusOneOrderItem(
        //        order.Id,
        //        order.OrderNo,
        //        user?.UserName ?? string.Empty,
        //        product?.ProductName ?? order.ProductName,
        //        order.Quantity,
        //        order.UnitPrice * order.Quantity));
        //}

        var snapshot = queryCounter.Snapshot();

        var result = new NPlusOneDemoResult(
            OrderCount: ordersResult.Items.Count,
            TotalSqlCount: snapshot.TotalCount,
            SelectSqlCount: snapshot.SelectCount,
            NonQuerySqlCount: snapshot.NonQueryCount,
            ExpectedNPlusOneDescription: $"当前订单数 {ordersResult.Items.Count}，预期会出现约 2 + 2 次 SELECT。",
            //ExpectedNPlusOneDescription: $"当前订单数 {ordersResult.Items.Count}，预期会出现约 2 + 2N 次 SELECT。",
            SqlSamples: snapshot.Commands.Take(20).ToList(),
            Items: items);

        return Result<NPlusOneDemoResult>.Success(result);
    }
}

public sealed class AppUsersByIdsSpecification : Specification<AppUser>
{
    public AppUsersByIdsSpecification(IEnumerable<long> userIds)
    {
        var ids = userIds.Distinct().ToArray();

        AddCriteria(user => ids.Contains(user.Id));
    }
}

public sealed class ProductsByIdsSpecification : Specification<Product>
{
    public ProductsByIdsSpecification(IEnumerable<long> productIds)
    {
        var ids = productIds.Distinct().ToArray();

        AddCriteria(product => ids.Contains(product.Id));
    }
}

public sealed record NPlusOneDemoResult(
    int OrderCount,
    int TotalSqlCount,
    int SelectSqlCount,
    int NonQuerySqlCount,
    string ExpectedNPlusOneDescription,
    IReadOnlyList<string> SqlSamples,
    IReadOnlyList<NPlusOneOrderItem> Items);

public sealed record NPlusOneOrderItem(
    long OrderId,
    string OrderNo,
    string UserName,
    string ProductName,
    decimal Quantity,
    decimal TotalAmount);

