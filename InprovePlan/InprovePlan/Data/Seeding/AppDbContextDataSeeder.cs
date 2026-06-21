using Bogus;
using InprovePlan.Domain.Entities;
using Instructure.Data;
using Instructure.Interfaces;
using Instructure.IResult;
using Microsoft.EntityFrameworkCore;

namespace InprovePlan.Data.Seeding;

/// <summary>
/// 开发/测试环境数据种子。
///
/// 设计目标：
/// 1. 只用于开发/测试环境，不建议在生产环境自动执行。
/// 2. 只在“空库”场景下执行一次；如果数据库中已存在业务数据，则直接跳过。
/// 3. 生成慢 SQL 演练需要的基础数据规模：
///    - AppUser      : 3,000
///    - UserAddress  : 12,000
///    - ProductType  : 40
///    - Product      : 5,000
///    - AppOrder     : 10,000
/// 4. 显式生成 Id，避免 ValueGeneratedNever 场景下因 Id=0 导致 EF Core 跟踪冲突。
/// 5. 通过分批提交减少一次性跟踪过多实体带来的内存压力。
/// </summary>
public sealed class AppDbContextDataSeeder(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IIdGenerator idGenerator)
{
    private const int UserCount = 3_000;
    private const int UserAddressCount = 12_000;
    private const int ProductTypeCount = 40;
    private const int ProductCount = 5_000;
    private const int OrderCount = 100_000;
    private const int BatchSize = 1_000;

    private const string DefaultPassword = "Password123?";
    private const string Currency = "CNY";

    /// <summary>
    /// 执行种子数据初始化。
    ///
    /// 规则：
    /// 1. 只要数据库中任意一张业务表已有数据，就直接跳过。
    /// 2. 只有在“空库”场景下，才执行整套造数逻辑。
    /// 3. 这样可以避免每次项目启动都继续插入数据。
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // 固定随机种子，使每次生成的数据分布尽量稳定，便于测试复现。
        Randomizer.Seed = new Random(20260617);

        // 只要已有任何业务数据，就认为当前库不是“初始空库”，直接跳过。
        if (await HasAnyBusinessDataAsync(cancellationToken))
        {
            return;
        }

        // 1. 先生成用户。
        // 商品和订单都依赖用户，所以用户必须先落库。
        var users = CreateUsers(UserCount);
        await InsertInBatchesAsync(users, cancellationToken);

        // 2. 再生成地址。
        // 订单中的 AddressId 会引用地址数据。
        var addresses = CreateUserAddresses(UserAddressCount);
        await InsertInBatchesAsync(addresses, cancellationToken);

        // 3. 再生成商品分类。
        // 商品依赖 ProductTypeId，所以分类要先准备好。
        var productTypes = CreateProductTypes(ProductTypeCount);
        await InsertInBatchesAsync(productTypes, cancellationToken);

        // 4. 再生成商品。
        var products = CreateProducts(ProductCount, users, productTypes);
        await InsertInBatchesAsync(products, cancellationToken);

        // 5. 最后生成订单。
        // 订单依赖用户、商品、地址，所以放在最后。
        var orders = CreateOrders(OrderCount, users, products, addresses);
        await InsertInBatchesAsync(orders, cancellationToken);
    }

    /// <summary>
    /// 判断数据库中是否已经存在业务数据。
    ///
    /// 说明：
    /// 1. 这里只判断“有没有数据”，不判断“数量是否达标”。
    /// 2. 你的诉求是“若数据库中有数据则不执行”，因此这里采用最严格的跳过策略。
    /// 3. 只要任意一张表已有记录，就直接返回 true。
    /// </summary>
    private async Task<bool> HasAnyBusinessDataAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Set<AppUser>().AnyAsync(cancellationToken)
            || await dbContext.Set<UserAddress>().AnyAsync(cancellationToken)
            || await dbContext.Set<ProductType>().AnyAsync(cancellationToken)
            || await dbContext.Set<Product>().AnyAsync(cancellationToken)
            || await dbContext.Set<AppOrder>().AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 生成用户数据。
    ///
    /// 满足 AppUserConfiguration：
    /// - UserName 唯一
    /// - Email 唯一
    /// - PhoneNumber 唯一
    /// - PasswordHash 必填
    /// - UserStatus 必填
    /// </summary>
    private List<AppUser> CreateUsers(int count)
    {
        var faker = new Faker("zh_CN");
        var passwordHash = passwordHasher.Hash(DefaultPassword);
        var users = new List<AppUser>(count);

        for (var index = 1; index <= count; index++)
        {
            var createdAt = RandomCreatedAt(faker);

            users.Add(new AppUser
            {
                Id = idGenerator.NewId(),
                UserName = $"seed_user_{index:D6}",
                Email = $"seed_user_{index:D6}@example.com",
                PhoneNumber = $"188{index:D8}",
                PasswordHash = passwordHash,
                Sex = faker.PickRandom<AppUserSex>(),
                UserStatus = index % 20 == 0
                    ? AppUserStatus.Frozen
                    : AppUserStatus.Enable,
                IsDeleted = false,
                DeletedAt = null,
                CreatedAt = createdAt,
                LastModifiedAt = createdAt.AddDays(faker.Random.Int(0, 90))
            });
        }

        return users;
    }

    /// <summary>
    /// 生成地址数据。
    ///
    /// 说明：
    /// 1. UserAddress 当前实体中没有 UserId，因此这里只生成独立地址数据。
    /// 2. 订单会随机引用这些地址 Id。
    /// 3. AddressTypeName 不要求唯一，但这里仍然带上序号，便于识别和排查。
    /// </summary>
    private List<UserAddress> CreateUserAddresses(int count)
    {
        var faker = new Faker("zh_CN");
        var aliases = new[]
        {
            "家",
            "公司",
            "父母家",
            "备用地址",
            "仓库",
            "门店自提点"
        };

        var addresses = new List<UserAddress>(count);

        for (var index = 1; index <= count; index++)
        {
            var createdAt = RandomCreatedAt(faker);

            addresses.Add(new UserAddress
            {
                Id = idGenerator.NewId(),
                AddressTypeName = $"{faker.PickRandom(aliases)}-{index:D6}",
                AddressStatus = index % 25 == 0
                    ? UserAddressStatus.Void
                    : UserAddressStatus.Enable,
                CreatedAt = createdAt,
                LastModifiedAt = createdAt.AddDays(faker.Random.Int(0, 60)),
                CreatedByUserId = null,
                LastModifiedByUserId = null
            });
        }

        return addresses;
    }

    /// <summary>
    /// 生成商品分类数据。
    ///
    /// 说明：
    /// 1. TypeName 设置为唯一，避免命中唯一索引冲突。
    /// 2. 分类数量不需要太大，40 个足够支撑商品筛选、聚合和关联查询演练。
    /// </summary>
    private List<ProductType> CreateProductTypes(int count)
    {
        var faker = new Faker("zh_CN");
        var productTypes = new List<ProductType>(count);

        for (var index = 1; index <= count; index++)
        {
            var createdAt = RandomCreatedAt(faker);

            productTypes.Add(new ProductType
            {
                Id = idGenerator.NewId(),
                TypeName = $"seed_type_{index:D3}",
                TypeDescription = $"慢 SQL 演练商品分类 {index:D3}",
                TypeStatus = index % 13 == 0
                    ? ProductTypeStatus.Void
                    : ProductTypeStatus.Enable,
                CreatedAt = createdAt,
                LastModifiedAt = createdAt.AddDays(faker.Random.Int(0, 45)),
                CreatedByUserId = null,
                LastModifiedByUserId = null
            });
        }

        return productTypes;
    }

    /// <summary>
    /// 生成商品数据。
    ///
    /// 说明：
    /// 1. ProductCode 唯一，满足唯一索引约束。
    /// 2. 每个商品随机挂到某个商品分类下。
    /// 3. CreatedByUserId / LastModifiedByUserId 引用已有用户，便于后续做审计或关联查询。
    /// </summary>
    private List<Product> CreateProducts(
        int count,
        IReadOnlyList<AppUser> users,
        IReadOnlyList<ProductType> productTypes)
    {
        if (users.Count == 0)
        {
            throw new InvalidOperationException("用户数据不能为空。");
        }

        if (productTypes.Count == 0)
        {
            throw new InvalidOperationException("商品分类数据不能为空。");
        }

        var faker = new Faker("zh_CN");
        var products = new List<Product>(count);

        for (var index = 1; index <= count; index++)
        {
            var createdAt = RandomCreatedAt(faker);
            var creator = PickOne(faker, users);
            var lastModifier = PickOne(faker, users);
            var productType = PickOne(faker, productTypes);
            var productId = idGenerator.NewId();

            products.Add(new Product
            {
                Id = productId,
                ProductCode = $"SP{productId}",
                ProductName = $"{faker.Commerce.ProductAdjective()}-{faker.Commerce.ProductMaterial()}-{index:D5}",
                ProductDescription = Truncate(
                    $"{faker.Commerce.ProductDescription()} | 演练商品序号:{index:D5}",
                    1000),
                ProductTypeId = productType.Id,
                ProductStatus = index % 37 == 0
                    ? AppProductStatus.SoldOut
                    : AppProductStatus.Enable,
                UnitPrice = Math.Round(faker.Random.Decimal(9.90m, 9_999.00m), 2),
                Currency = Currency,
                CreatedAt = createdAt,
                LastModifiedAt = createdAt.AddDays(faker.Random.Int(0, 30)),
                CreatedByUserId = creator.Id,
                LastModifiedByUserId = lastModifier.Id
            });
        }

        return products;
    }

    /// <summary>
    /// 生成订单数据。
    ///
    /// 说明：
    /// 1. 订单会引用真实用户、商品、地址，便于后续模拟 Join / Include / 分页查询。
    /// 2. 订单中保留商品快照字段 ProductName / ProductCode / UnitPrice / Currency。
    /// 3. 通过“热点用户 + 热点商品”分布，让部分数据更集中，更容易模拟真实查询热点。
    /// </summary>
    private List<AppOrder> CreateOrders(
        int count,
        IReadOnlyList<AppUser> users,
        IReadOnlyList<Product> products,
        IReadOnlyList<UserAddress> addresses)
    {
        if (users.Count == 0)
        {
            throw new InvalidOperationException("用户数据不能为空。");
        }

        if (products.Count == 0)
        {
            throw new InvalidOperationException("商品数据不能为空。");
        }

        if (addresses.Count == 0)
        {
            throw new InvalidOperationException("地址数据不能为空。");
        }

        var faker = new Faker("zh_CN");
        var orders = new List<AppOrder>(count);

        // 让前一小部分用户和商品成为“热点数据”，更接近真实业务分布。
        var hotUsers = users.Take(Math.Min(200, users.Count)).ToArray();
        var hotProducts = products.Take(Math.Min(500, products.Count)).ToArray();

        for (var index = 1; index <= count; index++)
        {
            var user = faker.Random.Bool(0.75f)
                ? PickOne(faker, hotUsers)
                : PickOne(faker, users);

            var product = faker.Random.Bool(0.80f)
                ? PickOne(faker, hotProducts)
                : PickOne(faker, products);

            var address = PickOne(faker, addresses);
            var occurredTime = RandomOccurredTime(faker);
            var quantity = Math.Round(faker.Random.Decimal(1m, 8m), 3);
            var cancelled = index % 23 == 0;

            var order = new AppOrder
            {
                Id = idGenerator.NewId(),
                OrderNo = $"SO{index:D10}",
                ProductId = product.Id,
                ProductName = product.ProductName,
                ProductCode = product.ProductCode,
                Currency = product.Currency,
                UnitPrice = product.UnitPrice,
                Quantity = quantity,
                UserId = user.Id,
                OccurredTime = occurredTime,
                OrderStatus = PickOrderStatus(faker, cancelled),
                AddressId = address.Id,
                Cancelled = cancelled,
                CreatedAt = occurredTime,
                LastModifiedAt = occurredTime.AddHours(faker.Random.Int(1, 72)),
                CreatedByUserId = user.Id,
                LastModifiedByUserId = user.Id,
                Product = null!,
                User = null!
            };

            // TotalAmount 是计算属性，需要显式回填。
            order.RecalculateTotalAmount();

            orders.Add(order);
        }

        return orders;
    }

    /// <summary>
    /// 分批插入数据。
    ///
    /// 说明：
    /// 1. 如果一次性插入 1 万以上实体，ChangeTracker 会持有大量对象，内存占用和保存耗时都会增加。
    /// 2. 分批保存后调用 ChangeTracker.Clear()，可以显著降低跟踪压力。
    /// </summary>
    private async Task InsertInBatchesAsync<T>(
        List<T> entities,
        CancellationToken cancellationToken)
        where T : class
    {
        for (var i = 0; i < entities.Count; i += BatchSize)
        {
            var take = Math.Min(BatchSize, entities.Count - i);
            var batch = entities.GetRange(i, take);

            await dbContext.Set<T>().AddRangeAsync(batch, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            // 清空跟踪，避免大批量插入时 ChangeTracker 持续膨胀。
            dbContext.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// 生成较早的创建时间。
    /// 用于让数据看起来不是同一时刻写入，便于后续按时间排序/分页测试。
    /// </summary>
    private static DateTimeOffset RandomCreatedAt(Faker faker)
    {
        var value = faker.Date.Between(
            DateTime.UtcNow.AddYears(-2),
            DateTime.UtcNow.AddDays(-7));

        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    /// <summary>
    /// 生成订单发生时间。
    /// 订单时间分布在近 18 个月，更适合做时间范围查询和分页排序测试。
    /// </summary>
    private static DateTimeOffset RandomOccurredTime(Faker faker)
    {
        var value = faker.Date.Between(
            DateTime.UtcNow.AddMonths(-18),
            DateTime.UtcNow);

        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    /// <summary>
    /// 按简单权重生成订单状态。
    ///
    /// 说明：
    /// 1. 已取消订单只在较早状态中产生，避免出现“已签收但又取消”这种明显不合理的数据。
    /// 2. 非取消订单则按一定分布生成，保证各种状态都有，但不会完全平均。
    /// </summary>
    private static AppOrderStatus PickOrderStatus(Faker faker, bool cancelled)
    {
        if (cancelled)
        {
            return faker.PickRandom(
                AppOrderStatus.Addition,
                AppOrderStatus.Paid);
        }

        var point = faker.Random.Int(1, 100);

        if (point <= 8)
        {
            return AppOrderStatus.Addition;
        }

        if (point <= 20)
        {
            return AppOrderStatus.Paid;
        }

        if (point <= 35)
        {
            return AppOrderStatus.Shipment;
        }

        if (point <= 53)
        {
            return AppOrderStatus.Transporting;
        }

        if (point <= 71)
        {
            return AppOrderStatus.Delivering;
        }

        if (point <= 86)
        {
            return AppOrderStatus.Delivered;
        }

        return AppOrderStatus.Received;
    }

    /// <summary>
    /// 从集合中随机取一个元素。
    /// </summary>
    private static T PickOne<T>(Faker faker, IReadOnlyList<T> items)
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("随机数据源不能为空。");
        }

        return items[faker.Random.Int(0, items.Count - 1)];
    }

    /// <summary>
    /// 截断字符串，避免超出数据库字段长度限制。
    /// </summary>
    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}