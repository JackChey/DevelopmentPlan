using InprovePlan.Domain.Entities;

namespace InprovePlan.ApiTests.Infrastructure;

/// <summary>
/// API 测试请求数据工厂。
///
/// 作用：
/// 1. 统一创建 Controller 请求体。
/// 2. 避免每个 API 测试重复写 anonymous object。
/// 3. 默认数据符合实体配置约束：
///    - ProductCode 不超过 64。
///    - ProductName 不超过 128。
///    - ProductDescription 不超过 1000。
///    - Currency 固定 3 位。
///    - Quantity 保留 3 位小数。
/// </summary>
public static class ApiTestDataFactory
{
    /// <summary>
    /// 创建用户注册请求。
    /// 用于先创建授权用户，再设置 _factory.CurrentUser.Id。
    /// </summary>
    public static object CreateUserRequest(
        string userName,
        string phoneNumber,
        string email)
    {
        return new
        {
            UserName = userName,
            Password = "Password123?",
            Sex = AppUserSex.Secret,
            PhoneNumber = phoneNumber,
            Email = email
        };
    }

    /// <summary>
    /// 创建新增商品请求。
    /// </summary>
    public static object CreateProductRequest(
        string productCode,
        string productName,
        int productTypeId = 1,
        decimal unitPrice = 99.99m,
        string currency = "CNY")
    {
        return new
        {
            ProductCode = productCode,
            ProductName = productName,
            ProductDescription = "符合 ProductConfiguration 长度要求的商品描述。",
            ProductTypeId = productTypeId,
            UnitPrice = unitPrice,
            Currency = currency
        };
    }

    /// <summary>
    /// 创建修改商品请求。
    /// </summary>
    public static object UpdateProductRequest(
        string productName = "api_product_updated",
        int productTypeId = 2,
        AppProductStatus productStatus = AppProductStatus.Enable,
        decimal unitPrice = 188.88m,
        string currency = "CNY")
    {
        return new
        {
            ProductName = productName,
            ProductDescription = "更新后的商品描述。",
            ProductTypeId = productTypeId,
            ProductStatus = productStatus,
            UnitPrice = unitPrice,
            Currency = currency
        };
    }

    /// <summary>
    /// 创建新增订单请求。
    /// </summary>
    public static object CreateOrderRequest(
        long productId,
        decimal quantity = 1.000m,
        long addressId = 10001)
    {
        return new
        {
            ProductId = productId,
            Quantity = quantity,
            AddressId = addressId
        };
    }

    /// <summary>
    /// 创建修改订单请求。
    /// </summary>
    public static object UpdateOrderRequest(
        decimal quantity = 2.345m,
        long addressId = 10002)
    {
        return new
        {
            Quantity = quantity,
            AddressId = addressId
        };
    }

    /// <summary>
    /// 创建修改订单状态请求。
    /// </summary>
    public static object ChangeOrderStatusRequest(
        AppOrderStatus orderStatus = AppOrderStatus.Paid)
    {
        return new
        {
            OrderStatus = orderStatus
        };
    }
}