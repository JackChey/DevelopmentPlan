using InprovePlan.Service.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;

namespace InprovePlan
{
    /// <summary>
    /// 注册插件依赖
    /// </summary>
    public static class DependenceInjection
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 注册 AutoMapper
            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(Assembly.GetExecutingAssembly());
            },Assembly.GetExecutingAssembly());

            AddInfranstructureServices(services, configuration);

            return services;
        }

        public static IServiceCollection AddInfranstructureServices(this IServiceCollection services,IConfiguration configuration)
        {
            // 获取 Jwt设置
            var configurationSection = configuration.GetSection("JwtSettings");
            var jwtsettings = configurationSection.Get<JwtSettings>();

            if (jwtsettings is null )
            {
                throw new NullReferenceException(nameof(JwtSettings));
            }

            // 注入jwtsetting
            services.Configure<JwtSettings>(configurationSection);

            ConfigureAuthentication(services, jwtsettings);

            return services;
        }

        public static IServiceCollection ConfigureAuthentication(this IServiceCollection services, JwtSettings jwtSettings)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(option =>
            {
                option.TokenValidationParameters = new()
                {
                    // 设置时钟偏移
                    ClockSkew = TimeSpan.Zero,

                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                };
            });

            return services;
        }
    }
}
