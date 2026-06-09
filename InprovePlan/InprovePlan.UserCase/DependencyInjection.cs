using InprovePlan.UserCase.AppUsers.Security;
using InprovePlan.UserCase.Behaviors;
using Instructure.IResult;
using Instructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace InprovePlan.UserCase;

public static class DependencyInjection
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddUserCaselService(this IServiceCollection services)
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

        return services;
    }
}
