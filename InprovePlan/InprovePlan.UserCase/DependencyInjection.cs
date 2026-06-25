using InprovePlan.UserCase.AppUsers.Security;
using InprovePlan.UserCase.Behaviors;
using InprovePlan.UserCase.Caching;
using Instructure.Caching;
using Instructure.IResult;
using Instructure.Repositories;
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

        services.AddMediatR(cfg =>
        {
            // 注册 UserCase 程序集下的 处理器
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

            // 注册授权验证
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));

            // 注册数据验证器
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // 注册 AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(Assembly.GetExecutingAssembly());
        });

        services.AddApplicationCache(configuration);

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

        services.AddSingleton<FusionCacheEventLogger>();

        services.AddHostedService(serviceProvider =>
                    serviceProvider.GetRequiredService<FusionCacheEventLogger>());

        return services;
    }
}
