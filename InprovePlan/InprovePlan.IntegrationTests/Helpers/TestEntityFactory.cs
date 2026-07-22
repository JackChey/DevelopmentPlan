using InprovePlan.Domain.Entities;
using Instructure.Interfaces;
using Instructure.IResult;

namespace InprovePlan.IntegrationTests.Helpers;

/// <summary>
/// 测试实体工厂，提供静态方法用于快速创建测试所需的领域实体实例。
/// 该类内部使用，主要用于单元测试或集成测试中构造默认或自定义的测试数据。
/// </summary>
internal static class TestEntityFactory
{
    /// <summary>
    /// 创建一个 AppUser 实例。
    /// 如果未提供特定参数，将生成默认的随机数据（如基于 ID 生成的用户名、邮箱和手机号）。
    /// 密码会自动通过提供的哈希器进行哈希处理。
    /// </summary>
    /// <param name="idGenerator">ID 生成器，用于生成唯一用户 ID。</param>
    /// <param name="passwordHasher">密码哈希器，用于对明文密码进行哈希。</param>
    /// <param name="id">可选的用户 ID。如果为 null，则自动生成新 ID。</param>
    /// <param name="userName">可选的用户名。如果为 null，则根据 ID 生成默认用户名。</param>
    /// <param name="password">明文密码，默认为 "Password123!"。</param>
    /// <param name="status">用户状态，默认为启用 (Enable)。</param>
    /// <param name="sex">性别，默认为保密 (Secret)。</param>
    /// <param name="isDeleted">是否已删除，默认为 false。</param>
    /// <returns>配置好的 AppUser 实例。</returns>
    public static AppUser CreateUser(
        IIdGenerator idGenerator,
        IPasswordHasher passwordHasher,
        long? id = null,
        string? userName = null,
        string password = "Password123!",
        AppUserStatus status = AppUserStatus.Enable,
        AppUserSex sex = AppUserSex.Secret,
        bool isDeleted = false)
    {
        // 如果未提供 ID，则使用生成器创建新 ID；否则使用提供的 ID
        var finalId = id ?? idGenerator.NewId();

        return new AppUser
        {
            Id = finalId,
            // 如果未提供用户名，则生成默认格式：user{ID}
            UserName = userName ?? $"user{finalId}",
            // 生成默认邮箱：user{ID}@example.com
            Email = $"user{finalId}@example.com",
            // 生成默认手机号：13 + ID的后9位（补零）
            PhoneNumber = $"13{Math.Abs(finalId % 1000000000):D9}",
            // 对明文密码进行哈希处理
            PasswordHash = passwordHasher.Hash(password),
            Sex = sex,
            UserStatus = status,
            IsDeleted = isDeleted,
            // 如果标记为删除，设置删除时间为当前 UTC 时间；否则为 null
            DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// 创建一个 Product 实例。
    /// 如果未提供特定参数，将生成默认的随机数据（如基于 ID 生成的产品代码和名称）。
    /// 货币代码会自动转换为大写，单价会保留两位小数。
    /// </summary>
    /// <param name="idGenerator">ID 生成器，用于生成唯一产品 ID。</param>
    /// <param name="id">可选的产品 ID。如果为 null，则自动生成新 ID。</param>
    /// <param name="code">可选的产品代码。如果为 null，则根据 ID 生成默认代码。</param>
    /// <param name="name">可选的产品名称。如果为 null，则根据 ID 生成默认名称。</param>
    /// <param name="status">产品状态，默认为启用 (Enable)。</param>
    /// <param name="productTypeId">产品类型 ID，默认为 1001。</param>
    /// <param name="unitPrice">单价，默认为 19.80m。</param>
    /// <param name="currency">货币类型，默认为 "RMB"。</param>
    /// <returns>配置好的 Product 实例。</returns>
    public static Product CreateProduct(
        IIdGenerator idGenerator,
        long? id = null,
        string? code = null,
        string? name = null,
        AppProductStatus status = AppProductStatus.Enable,
        long productTypeId = 1001,
        decimal unitPrice = 19.80m,
        string currency = "RMB")
    {
        // 如果未提供 ID，则使用生成器创建新 ID；否则使用提供的 ID
        var finalId = id ?? idGenerator.NewId();

        return new Product
        {
            Id = finalId,
            // 如果未提供代码，则生成默认格式：P{ID}
            ProductCode = code ?? $"P{finalId}",
            // 如果未提供名称，则生成默认格式：Product {ID}
            ProductName = name ?? $"Product {finalId}",
            // 生成默认描述
            ProductDescription = $"Product {finalId} description",
            ProductTypeId = productTypeId,
            ProductStatus = status,
            // 确保单价保留两位小数
            UnitPrice = Math.Round(unitPrice, 2),
            // 确保货币代码为大写不变形式
            Currency = currency.ToUpperInvariant(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// 创建一个 AppOrder 实例。
    /// 订单信息基于提供的用户和产品实体自动填充（如产品名称、代码、单价、货币等）。
    /// 创建后会自动重新计算订单总金额。
    /// </summary>
    /// <param name="idGenerator">ID 生成器，用于生成唯一订单 ID。</param>
    /// <param name="user">下单用户实体，用于获取用户 ID。</param>
    /// <param name="product">购买产品实体，用于获取产品相关信息及计算金额。</param>
    /// <param name="id">可选的订单 ID。如果为 null，则自动生成新 ID。</param>
    /// <param name="status">订单状态，默认为新增 (Addition)。</param>
    /// <param name="cancelled">是否已取消，默认为 false。</param>
    /// <param name="quantity">购买数量，默认为 2.000m。</param>
    /// <param name="addressId">收货地址 ID，默认为 10001。</param>
    /// <returns>配置好并计算了总金额的 AppOrder 实例。</returns>
    public static AppOrder CreateOrder(
        IIdGenerator idGenerator,
        AppUser user,
        Product product,
        long? id = null,
        AppOrderStatus status = AppOrderStatus.Addition,
        bool cancelled = false,
        decimal quantity = 2.000m,
        long addressId = 10001)
    {
        // 如果未提供 ID，则使用生成器创建新 ID；否则使用提供的 ID
        var finalId = id ?? idGenerator.NewId();

        var order = new AppOrder
        {
            Id = finalId,
            // 生成默认订单号：O{ID}
            OrderNo = $"O{finalId}",
            // 从产品实体复制相关信息
            ProductId = product.Id,
            ProductName = product.ProductName,
            ProductCode = product.ProductCode,
            Currency = product.Currency,
            UnitPrice = product.UnitPrice,
            Quantity = quantity,
            // 从用户实体获取用户 ID
            UserId = user.Id,
            OccurredTime = DateTimeOffset.UtcNow,
            OrderStatus = status,
            Cancelled = cancelled,
            AddressId = addressId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // 调用业务方法重新计算订单总金额（通常基于单价 * 数量）
        order.RecalculateTotalAmount();

        return order;
    }
}

