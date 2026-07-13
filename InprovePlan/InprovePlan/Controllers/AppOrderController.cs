using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.AppOrders.Commands;
using InprovePlan.UserCase.AppOrders.Queries;
using Instructure.Paging;
using Instructure.Sorting;
using Microsoft.AspNetCore.Mvc;

namespace InprovePlan.Controllers;

/// <summary>
/// 订单业务接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AppOrderController() : BaseController
{
    /// <summary>
    /// 新增订单请求。
    /// </summary>
    /// <param name="ProductId">商品Id。</param>
    /// <param name="Quantity">购买数量。</param>
    /// <param name="AddressId">收货地址Id。</param>
    public sealed record CreateAppOrderRequest(
        long ProductId,
        decimal Quantity,
        long AddressId);

    /// <summary>
    /// 新增订单请求。
    /// </summary>
    /// <param name="ProductId">商品Id。</param>
    /// <param name="Quantity">购买数量。</param>
    /// <param name="AddressId">收货地址Id。</param>
    public sealed record CreateAppOrderWithIdempotencyRequest(
        long ProductId,
        decimal Quantity,
        long AddressId);

    /// <summary>
    /// 修改订单请求。
    /// </summary>
    /// <param name="Quantity">购买数量。</param>
    /// <param name="AddressId">收货地址Id。</param>
    public sealed record UpdateAppOrderRequest(
        decimal Quantity,
        long AddressId);

    /// <summary>
    /// 修改订单状态请求。
    /// </summary>
    /// <param name="OrderStatus">订单状态。</param>
    public sealed record ChangeAppOrderStatusRequest(
        AppOrderStatus OrderStatus);

    /// <summary>
    /// 修改订单状态请求。
    /// </summary>
    /// <param name="OrderStatus">订单状态。</param>
    /// <param name="UpdateReason">修改理由。</param>
    public sealed record ChangeAppOrderStatusWithIdempotencyAndMqRequest(
        AppOrderStatus OrderStatus,
        string UpdateReason);

    /// <summary>
    /// 创建订单。
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAppOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CreateAppOrderCommand(
                request.ProductId,
                request.Quantity,
                request.AddressId),
            cancellationToken);

        return ReturnResult(result);
    }

    /// <summary>
    /// 幂等创建订单。
    /// </summary>
    [HttpPost("CreateWithIdempotency")]
    public async Task<IActionResult> CreateWithIdempotency(
        [FromBody] CreateAppOrderWithIdempotencyRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CreateAppOrderWithIdempotencyCommand(
                request.ProductId,
                request.Quantity,
                request.AddressId,
                idempotencyKey ?? ""),
            cancellationToken);

        return ReturnResult(result);
    }

    /// <summary>
    /// 修改订单。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateAppOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateAppOrderCommand(
                id,
                request.Quantity,
                request.AddressId),
            cancellationToken);

        return ReturnResult(result);
    }

    /// <summary>
    /// 修改订单状态。
    /// </summary>
    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> ChangeStatusWithIdempotencyAndMq(
        long id,
        [FromBody] ChangeAppOrderStatusWithIdempotencyAndMqRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ChangeAppOrderStatusWithIdempotencyAndMqCommand(
                id,
                request.OrderStatus,
                request.UpdateReason,
                idempotencyKey ?? ""),
            cancellationToken);

        return ReturnResult(result);
    }

    /// <summary>
    /// 删除订单。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new DeleteAppOrderCommand(id),
            cancellationToken);

        return ReturnResult(result);
    }

    /// <summary>
    /// 查询单个订单。
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new GetAppOrderByIdQuery(id),
            cancellationToken);

        return ReturnResult(result);
    }

    /// <summary>
    /// 分页查询订单。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int pageIndex = Pagination.DefaultPageIndex,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] string? keyword = null,
        [FromQuery] long? userId = null,
        [FromQuery] long? productId = null,
        [FromQuery] AppOrderStatus? orderStatus = null,
        [FromQuery] DateTimeOffset? startTime = null,
        [FromQuery] DateTimeOffset? endTime = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new GetAppOrdersPagedQuery(
                new Pagination
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize
                },
                new SortQuery
                {
                    SortBy = sortBy,
                    SortDirection = sortDirection
                },
                keyword,
                userId,
                productId,
                orderStatus,
                startTime,
                endTime),
            cancellationToken);

        return ReturnResult(result);
    }

    /// <summary>
    /// 查询订单。用于测试 N+1 问题
    /// </summary>
    [NonAction]
    [HttpGet("GetTest")]
    public async Task<IActionResult> GetTest(
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new GetAppOrderTestQuery(),
            cancellationToken);

        return ReturnResult(result);
    }

    /// <summary>
    /// 查询订单。用于测试对比 NoTracking 问题
    /// </summary>
    [NonAction]
    [HttpGet("GetWithNotrackingTest")]
    public async Task<IActionResult> GetWithNotrackingTest(
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new GetAppOrderWithNotrackingTestQuery(),
            cancellationToken);

        return ReturnResult(result);
    }

    /// <summary>
    /// 查询订单。用于测试对比 NoTracking 问题
    /// </summary>
    [NonAction]
    [HttpGet("GetWithtrackingTest")]
    public async Task<IActionResult> GetWithtrackingTest(
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new GetAppOrderWithtrackingTestQuery(),
            cancellationToken);

        return ReturnResult(result);
    }

    /// <summary>
    /// 根据用户ID查询订单
    /// 用于模拟慢Sql查询--无索引的情况
    /// </summary>
    /// <param name="Id">用户ID</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [NonAction]
    [HttpGet("GetSlowSqlWithNoIndexTest")]
    public async Task<IActionResult> GetSlowSqlWithNoIndexTest(long Id,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new GetAppOrderSlowSqlWithNoIndexTestQuery(Id),
            cancellationToken);

        return ReturnResult(result);
    }
}
