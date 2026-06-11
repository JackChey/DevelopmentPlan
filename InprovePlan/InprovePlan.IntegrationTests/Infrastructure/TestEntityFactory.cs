using InprovePlan.Domain.Entities;
using Instructure.Interfaces;
using Instructure.IResult;

namespace InprovePlan.IntegrationTests.Infrastructure;

/// <summary>
/// 集成测试实体工厂。
///
/// 所有实体显式设置 Id，
/// 避免 ValueGeneratedNever 下 Id = 0 导致跟踪冲突。
/// </summary>
public static class TestEntityFactory
{
    public static AppUser CreateUser(
        IIdGenerator idGenerator,
        IPasswordHasher passwordHasher,
        string userName,
        string email,
        string phoneNumber,
        AppUserStatus status = AppUserStatus.Enable,
        bool isDeleted = false)
    {
        return new AppUser
        {
            Id = idGenerator.NewId(),
            UserName = userName,
            Email = email,
            PhoneNumber = phoneNumber,
            PasswordHash = passwordHasher.Hash("Password123?"),
            Sex = AppUserSex.Secret,
            UserStatus = status,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// 创建商品实体。
    /// </summary>
    public static Product CreateProduct(
        IIdGenerator idGenerator,
        string productCode,
        string productName,
        int productTypeId = 1,
        AppProductStatus status = AppProductStatus.Enable,
        decimal unitPrice = 99.99m,
        string currency = "CNY")
    {
        return new Product
        {
            Id = idGenerator.NewId(),
            ProductCode = productCode,
            ProductName = productName,
            ProductDescription = "符合 ProductConfiguration 长度要求的商品描述。",
            ProductTypeId = productTypeId,
            ProductStatus = status,
            UnitPrice = Math.Round(unitPrice, 2),
            Currency = currency.ToUpperInvariant(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// 创建订单实体。
    /// 订单会保存商品快照，避免商品后续变化影响历史订单。
    /// </summary>
    public static AppOrder CreateOrder(
        IIdGenerator idGenerator,
        AppUser user,
        Product product,
        string orderNo,
        decimal quantity = 1.000m,
        long addressId = 10001,
        AppOrderStatus status = AppOrderStatus.Addition,
        bool cancelled = false,
        DateTimeOffset? occurredTime = null)
    {
        var order = new AppOrder
        {
            Id = idGenerator.NewId(),
            OrderNo = orderNo,
            ProductId = product.Id,
            ProductName = product.ProductName,
            ProductCode = product.ProductCode,
            Currency = product.Currency,
            UnitPrice = product.UnitPrice,
            Quantity = Math.Round(quantity, 3),
            UserId = user.Id,
            OccurredTime = occurredTime ?? DateTimeOffset.UtcNow,
            OrderStatus = status,
            Cancelled = cancelled,
            AddressId = addressId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        order.RecalculateTotalAmount();

        return order;
    }
}