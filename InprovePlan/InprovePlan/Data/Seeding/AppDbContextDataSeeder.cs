using Bogus;
using InprovePlan.Domain.Entities;
using Instructure.Data;
using Instructure.Interfaces;
using Instructure.IResult;
using Microsoft.EntityFrameworkCore;

namespace InprovePlan.Data.Seeding;

/// <summary>
/// 应用开发/测试数据种子。
///
/// 设计目标：
/// 1. 只用于开发/测试环境。
/// 2. 手动生成雪花 Id，避免批量 Add 时多个实体 Id = 0 导致 EF Core 跟踪冲突。
/// 3. 用户名、邮箱、手机号、商品编码、订单号全部生成稳定唯一值，避免唯一索引冲突。
/// 4. 生成顺序为：用户 -> 商品 -> 订单，保证外键有效。
/// </summary>
public sealed class AppDbContextDataSeeder(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IIdGenerator idGenerator)
{
    private const string DefaultPassword = "Password123?";
    private const string Currency = "CNY";

    /// <summary>
    /// 执行数据初始化。
    ///
    /// 幂等规则：
    /// 只要用户表已有数据，就认为种子数据已经执行过，直接跳过。
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Set<AppUser>().AnyAsync(cancellationToken))
        {
            return;
        }

        var users = CreateUsers(count: 50);

        await dbContext.Set<AppUser>().AddRangeAsync(users, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var products = CreateProducts(count: 100, users);

        await dbContext.Set<Product>().AddRangeAsync(products, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var orders = CreateOrders(count: 300, users, products);

        await dbContext.Set<AppOrder>().AddRangeAsync(orders, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 生成用户。
    ///
    /// 满足 AppUserConfiguration：
    /// - Id：手动生成，避免 Id = 0 重复跟踪。
    /// - PasswordHash：必填，长度小于 255。
    /// - UserName：必填，唯一，长度小于 64。
    /// - PhoneNumber：必填，唯一，长度小于 32。
    /// - Email：必填，唯一，长度小于 128。
    /// - Sex：必填。
    /// - UserStatus：必填。
    /// </summary>
    private List<AppUser> CreateUsers(int count)
    {
        var passwordHash = passwordHasher.Hash(DefaultPassword);

        var users = new List<AppUser>(count);
        var faker = new Faker("zh_CN");

        for (var index = 1; index <= count; index++)
        {
            var id = idGenerator.NewId();

            users.Add(new AppUser
            {
                Id = id,
                UserName = $"seed_user_{index:D4}",
                Email = $"seed_user_{index:D4}@example.com",
                PhoneNumber = $"139{index:D8}",
                PasswordHash = passwordHash,
                Sex = faker.PickRandom<AppUserSex>(),
                UserStatus = index % 10 == 0
                    ? AppUserStatus.Frozen
                    : AppUserStatus.Enable,
                IsDeleted = false,
                DeletedAt = null
            });
        }

        return users;
    }

    /// <summary>
    /// 生成商品。
    ///
    /// 满足 ProductConfiguration：
    /// - Id：手动生成。
    /// - ProductCode：必填，唯一，长度小于 64。
    /// - ProductName：必填，长度小于 128。
    /// - ProductDescription：必填，长度小于 1000。
    /// - ProductTypeId：必填。
    /// - ProductStatus：必填。
    /// - UnitPrice：必填，decimal(18,2)。
    /// - Currency：必填，长度 3。
    /// - CreatedByUserId：引用已有用户。
    /// </summary>
    private List<Product> CreateProducts(
        int count,
        IReadOnlyList<AppUser> users)
    {
        var products = new List<Product>(count);
        var faker = new Faker("zh_CN");

        for (var index = 1; index <= count; index++)
        {
            var creator = PickOne(faker, users);

            products.Add(new Product
            {
                Id = idGenerator.NewId(),
                ProductCode = $"P{index:D8}",
                ProductName = $"测试商品-{index:D4}",
                ProductDescription = faker.Commerce.ProductDescription(),
                ProductTypeId = faker.Random.Int(1, 10),
                ProductStatus = index % 12 == 0
                    ? AppProductStatus.SoldOut
                    : AppProductStatus.Enable,
                UnitPrice = Math.Round(faker.Random.Decimal(9.9m, 9999m), 2),
                Currency = Currency,
                CreatedByUserId = creator.Id,
                LastModifiedByUserId = null
            });
        }

        return products;
    }

    /// <summary>
    /// 生成订单。
    ///
    /// 满足 AppOrderConfiguration：
    /// - Id：手动生成。
    /// - OrderNo：必填，唯一，长度小于 64。
    /// - ProductId：引用已有商品。
    /// - ProductName/ProductCode：商品快照，必填。
    /// - Currency：必填，长度 3。
    /// - UnitPrice：必填，decimal(18,2)。
    /// - Quantity：必填，decimal(18,3)。
    /// - UserId：引用已有用户。
    /// - OccurredTime：必填。
    /// - OrderStatus：必填。
    /// - AddressId：必填。
    /// - CreatedByUserId：引用已有用户。
    /// </summary>
    private List<AppOrder> CreateOrders(
        int count,
        IReadOnlyList<AppUser> users,
        IReadOnlyList<Product> products)
    {
        var orders = new List<AppOrder>(count);
        var faker = new Faker("zh_CN");

        for (var index = 1; index <= count; index++)
        {
            var user = PickOne(faker, users);
            var product = PickOne(faker, products);
            var quantity = Math.Round(faker.Random.Decimal(1m, 5m), 3);

            var order = new AppOrder
            {
                Id = idGenerator.NewId(),
                OrderNo = $"O{DateTimeOffset.UtcNow:yyyyMMdd}{index:D10}",
                ProductId = product.Id,
                ProductName = product.ProductName,
                ProductCode = product.ProductCode,
                Currency = product.Currency,
                UnitPrice = product.UnitPrice,
                Quantity = quantity,
                UserId = user.Id,
                OccurredTime = faker.Date.PastOffset(1),
                OrderStatus = faker.PickRandom<AppOrderStatus>(),
                AddressId = faker.Random.Long(1, 10_000),
                CreatedByUserId = user.Id,
                LastModifiedByUserId = null,
                Product = null!,
                User = null!
            };

            order.RecalculateTotalAmount();

            orders.Add(order);
        }

        return orders;
    }

    /// <summary>
    /// 从集合中随机取一个元素。
    /// </summary>
    private static T PickOne<T>(
        Faker faker,
        IReadOnlyList<T> items)
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("随机数据源不能为空。");
        }

        return items[faker.Random.Int(0, items.Count - 1)];
    }
}