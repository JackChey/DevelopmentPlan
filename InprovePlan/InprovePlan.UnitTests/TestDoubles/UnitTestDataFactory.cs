using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.AppOrders.Commands;
using InprovePlan.UserCase.AppOrders.Queries;
using InprovePlan.UserCase.Products.Commands;
using InprovePlan.UserCase.Products.Queries;
using Instructure.Paging;
using Instructure.Sorting;

namespace InprovePlan.UnitTests.TestDoubles;

/// <summary>
/// 单元测试 Command / Query 数据工厂。
///
/// 默认返回合法对象。
/// 每个测试只修改自己关心的字段，降低重复代码。
/// </summary>
public static class UnitTestDataFactory
{
    public static CreateProductCommand CreateProductCommand(
        string productCode = "UNIT-PRODUCT-001",
        string productName = "unit_product_001",
        string productDescription = "符合长度要求的商品描述。",
        int productTypeId = 1,
        decimal unitPrice = 99.99m,
        string currency = "CNY")
        => new(productCode, productName, productDescription, productTypeId, unitPrice, currency);

    public static UpdateProductCommand UpdateProductCommand(
        long id = 1,
        string productName = "unit_product_updated",
        string productDescription = "更新后的商品描述。",
        int productTypeId = 1,
        AppProductStatus productStatus = AppProductStatus.Enable,
        decimal unitPrice = 99.99m,
        string currency = "CNY")
        => new(id, productName, productDescription, productTypeId, productStatus, unitPrice, currency);

    public static DeleteProductCommand DeleteProductCommand(long id = 1)
        => new(id);

    public static GetProductsPagedQuery GetProductsPagedQuery(
        int pageIndex = 1,
        int pageSize = 10,
        int? productTypeId = 1,
        AppProductStatus? productStatus = AppProductStatus.Enable)
        => new(
            new Pagination { PageIndex = pageIndex, PageSize = pageSize },
            new SortQuery { SortBy = "createdAt", SortDirection = "desc" },
            "product",
            productTypeId,
            productStatus,
            false);

    public static CreateAppOrderCommand CreateAppOrderCommand(
        long productId = 1,
        decimal quantity = 1.000m,
        long addressId = 10001)
        => new(productId, quantity, addressId);

    public static UpdateAppOrderCommand UpdateAppOrderCommand(
        long id = 1,
        decimal quantity = 2.345m,
        long addressId = 10002)
        => new(id, quantity, addressId);

    public static DeleteAppOrderCommand DeleteAppOrderCommand(long id = 1)
        => new(id);

    public static ChangeAppOrderStatusCommand ChangeAppOrderStatusCommand(
        long id = 1,
        AppOrderStatus orderStatus = AppOrderStatus.Paid)
        => new(id, orderStatus);

    public static GetAppOrderByIdQuery GetAppOrderByIdQuery(long id = 1)
        => new(id);

    public static GetAppOrdersPagedQuery GetAppOrdersPagedQuery(
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null)
        => new(
            new Pagination { PageIndex = 1, PageSize = 10 },
            new SortQuery { SortBy = "createdAt", SortDirection = "desc" },
            "O20260611",
            1,
            1,
            AppOrderStatus.Addition,
            startTime ?? DateTimeOffset.UtcNow.AddDays(-1),
            endTime ?? DateTimeOffset.UtcNow.AddDays(1));
}