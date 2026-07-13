using InprovePlan.ShareKernel.Contracts;
using InprovePlan.ShareKernel.Messaging;
using InprovePlan.UserCase.AppUsers.Security;
using InprovePlan.UserCase.Behaviors;
using InprovePlan.UserCase.Caching;
using InprovePlan.UserCase.Customers;
using InprovePlan.UserCase.Idempotency;
using InprovePlan.UserCase.Messaging;
using Instructure.Caching;
using Instructure.Data;
using Instructure.Idempotency;
using Instructure.Interfaces;
using Instructure.IResult;
using Instructure.Repositories;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System.Reflection;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

namespace InprovePlan.UserCase;

public static class DependencyInjection
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddUserCaselService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PasswordHasherOptions>(options =>
        {
            // Identity V3 是 ASP.NET Core Identity 当前主流格式。
            options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;

            // 迭代次数越高越安全，但登录/注册耗时也会增加。
            // 可根据服务器性能压测后调整。
            options.IterationCount = 100_000;
        });

        services.AddScoped<IPasswordHasher, AppUserPasswordHasher>();

        services.AddScoped(typeof(IReadRepository<>), typeof(EfReadRepository<>));
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddMediatR(cfg =>
        {
            // 注册 UserCase 程序集下的 处理器
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

            // 注册授权验证
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));

            // 注册数据验证器
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
        });

        // 注册 AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(Assembly.GetExecutingAssembly());
        });

        services.AddApplicationCache(configuration);

        services.AddIdempotency(configuration);
        services.AddRabbitMq(configuration);

        return services;
    }

    /// <summary>
    /// 配置二级缓存
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    /// <exception cref="ValidationException"></exception>
    public static IServiceCollection AddApplicationCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CacheOptions>(configuration.GetSection("Cache"));

        var redisConnection = configuration.GetConnectionString("RedisConnection");

        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            throw new ValidationException(new Dictionary<string, string[]>() { { "验证异常", new string[] { "RedisConnection is not configured." } } });
        }

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            // ConnectionMultiplexer 应该复用，不要每次请求创建。
            return ConnectionMultiplexer.Connect(redisConnection);
        });

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;

            // 建议设置实例前缀，避免多个系统共用 Redis 时 Key 混乱。
            options.InstanceName = "InprovePlan:";
        });

        services.AddFusionCache()
            .WithDefaultEntryOptions(new FusionCacheEntryOptions()
                .SetDuration(TimeSpan.FromMinutes(5))
                .SetFailSafe(true, TimeSpan.FromMinutes(30))
                .SetFactoryTimeouts(
                    TimeSpan.FromMilliseconds(300),
                    TimeSpan.FromSeconds(2)))
            .WithSystemTextJsonSerializer()
            .WithDistributedCache(provider =>
            {
                return provider.GetRequiredService<IDistributedCache>();
            })
            .WithBackplane(new RedisBackplane(new RedisBackplaneOptions
            {
                Configuration = redisConnection
            }));

        services.AddSingleton<ICacheKeyBuilder, CacheKeyBuilder>();
        services.AddSingleton<IAppCache, AppCache>();
        services.AddSingleton<IRedisRepository, RedisRepository>();

        services.AddHostedService<FusionCacheEventLogger>();

        //services.AddSingleton<FusionCacheEventLogger>();

        //services.AddHostedService(serviceProvider =>
        //            serviceProvider.GetRequiredService<FusionCacheEventLogger>());

        return services;
    }

    /// <summary>
    /// 配置幂等
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    /// <exception cref="ValidationException"></exception>
    public static IServiceCollection AddIdempotency(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<IdempotencyOptions>(configuration.GetSection("Idempotency"));

        services.AddScoped<IIdempotencyService, IdempotencyService>();
        services.AddScoped<IRequestHashProvider, RequestHashProvider>();
        services.AddScoped<IDistributedLock, RedisDistributedLock>();
        services.AddScoped<IIdempotencyRecordRepository, IdempotencyRecordRepository>();

        return services;
    }

    /// <summary>
    /// 配置 RabbitMq
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddRabbitMq(
       this IServiceCollection services,
       IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            // 注册消费者。
            // 后续库存、优惠券、通知等消费者都可以在这里继续注册。
            x.AddConsumer<OrderStatusChangedConsumer>();

            // EF Outbox。
            // 作用：
            // 1. API 事务内保存消息。
            // 2. 后台任务可靠发送 RabbitMQ。
            // 3. 避免业务数据和 MQ 消息不一致。
            x.AddEntityFrameworkOutbox<AppDbContext>(o =>
            {
                // Outbox 查询间隔。
                // 值越小，消息越快发出，但数据库压力也越高。
                o.QueryDelay = TimeSpan.FromSeconds(1);

                // 使用当前数据库类型对应的配置。
                // SQL Server 用 UseSqlServer。
                // PostgreSQL 用 UsePostgres。
                // MySQL 没有专用扩展时，通常使用通用配置并结合 Provider 测试。
                o.UseMySql();

                // Bus Outbox 表示当前服务通过 IPublishEndpoint 发布的消息先进入 Outbox。
                o.UseBusOutbox();
            });

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = configuration.GetSection("RabbitMq");

                cfg.Host(
                    rabbit["Host"],
                    ushort.Parse(rabbit["Port"] ?? "5672"),
                    rabbit["VirtualHost"],
                    h =>
                    {
                        h.Username(rabbit["Username"]??"");
                        h.Password(rabbit["Password"]??"");
                    });

                // 订单状态变更消费者队列。
                cfg.ReceiveEndpoint("order-status-changed-queue", e =>
                {
                    // 消费者并发数。
                    // 生产环境需要结合数据库压力、业务耗时、实例数量调整。
                    e.ConcurrentMessageLimit = 8;

                    // 单个消费者一次最多预取多少条消息。
                    // 避免消费者一次拿太多消息，服务宕机后造成大量重新投递。
                    e.PrefetchCount = 16;

                    // 消费失败重试。
                    // 这里只处理临时异常，例如数据库短暂不可用、网络抖动。
                    e.UseMessageRetry(r =>
                    {
                        r.Interval(6, TimeSpan.FromSeconds(10));
                    });

                    // 消费异常最终会进入 MassTransit 自动创建的 error queue。
                    // 例如：order-status-changed-queue_error
                    e.ConfigureConsumer<OrderStatusChangedConsumer>(context);
                });

                // 可选：统一事件实体名。
                // 这样 RabbitMQ 中的 exchange 名称更可控。
                cfg.Message<OrderStatusChangedEvent>(m =>
                {
                    m.SetEntityName("order.status.changed");
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IOrderEventPublisher, OrderEventPublisher>();

        return services;
    }
    
}
