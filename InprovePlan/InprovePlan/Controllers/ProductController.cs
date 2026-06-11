using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.Products.Commands;
using InprovePlan.UserCase.Products.Queries;
using Instructure.Paging;
using Instructure.Sorting;
using Microsoft.AspNetCore.Mvc;

namespace InprovePlan.Controllers;

/// <summary>
/// 商品业务接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductController() : BaseController
{
    /// <summary>
    /// 新增商品请求。
    /// </summary>
    /// <param name="ProductCode">商品编码。</param>
    /// <param name="ProductName">商品名称。</param>
    /// <param name="ProductDescription">商品描述。</param>
    /// <param name="ProductTypeId">商品类型Id。</param>
    /// <param name="UnitPrice">商品单价。</param>
    /// <param name="Currency">币种。</param>
    public sealed record CreateProductRequest(
        string ProductCode,
        string ProductName,
        string ProductDescription,
        int ProductTypeId,
        decimal UnitPrice,
        string Currency);

    /// <summary>
    /// 修改商品请求。
    /// </summary>
    /// <param name="ProductName">商品名称。</param>
    /// <param name="ProductDescription">商品描述。</param>
    /// <param name="ProductTypeId">商品类型Id。</param>
    /// <param name="ProductStatus">商品状态。</param>
    /// <param name="UnitPrice">商品单价。</param>
    /// <param name="Currency">币种。</param>
    public sealed record UpdateProductRequest(
        string ProductName,
        string ProductDescription,
        int ProductTypeId,
        AppProductStatus ProductStatus,
        decimal UnitPrice,
        string Currency);

    /// <summary>
    /// 新增商品。
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CreateProductCommand(
                request.ProductCode,
                request.ProductName,
                request.ProductDescription,
                request.ProductTypeId,
                request.UnitPrice,
                request.Currency),
            cancellationToken);

        return ReturnResult(result);
    }

    /// <summary>
    /// 修改商品。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateProductCommand(
                id,
                request.ProductName,
                request.ProductDescription,
                request.ProductTypeId,
                request.ProductStatus,
                request.UnitPrice,
                request.Currency),
            cancellationToken);

        return ReturnResult(result);
    }

    /// <summary>
    /// 删除商品。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new DeleteProductCommand(id),
            cancellationToken);

        return ReturnResult(result);
    }

    /// <summary>
    /// 查询单个商品。
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new GetProductByIdQuery(id),
            cancellationToken);

        return ReturnResult(result);
    }

    /// <summary>
    /// 分页查询商品。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int pageIndex = Pagination.DefaultPageIndex,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] string? keyword = null,
        [FromQuery] int? productTypeId = null,
        [FromQuery] AppProductStatus? productStatus = null,
        [FromQuery] bool includeVoid = false,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new GetProductsPagedQuery(
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
                productTypeId,
                productStatus,
                includeVoid),
            cancellationToken);

        return ReturnResult(result);
    }
}
