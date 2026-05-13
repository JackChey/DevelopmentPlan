using InprovePlan.Connections;
using InprovePlan.IService.Jwt;
using InprovePlan.Service.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
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
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
        {
            ConfigEnvironment(services, configuration);

            // 注册 AutoMapper
            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(Assembly.GetExecutingAssembly());
            },Assembly.GetExecutingAssembly());

            AddInfranstructureServices(services, configuration);



            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
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

            services.AddTransient<IJwtService,JwtService>();

            ConfigureAuthentication(services, jwtsettings);

            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="jwtSettings"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 根据不同的配置环境读取配置文件
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection ConfigEnvironment(this IServiceCollection services, IConfiguration configuration)
        {
            // 获取当前配置环境
            var currentEnv = configuration.GetSection("Run_Environment").Get<string>();

            // 根据环境读取不同的配置文件
            var settingPath = Path.Combine(AppContext.BaseDirectory, $"appsettings_{currentEnv}.json");

            if (!File.Exists(settingPath))
            {
                throw new NullReferenceException($"当前环境:{currentEnv} 对应配置文件不存在");
            }

            var envSetting = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile(settingPath).Build();

            var dbConnectionStr = envSetting.GetSection("ConnectionStrings:DBConnection");
            var rediaConnectionStr = envSetting.GetSection("ConnectionStrings:RedisConnection");
            var rabbitMqConnectionStr = envSetting.GetSection("ConnectionStrings:RabbitMqConnection");

            services.Configure<DBConnection>(dbConnectionStr);
            services.Configure<RedisConnection>(rediaConnectionStr);
            services.Configure<RabbitMqConnection>(rabbitMqConnectionStr);

            return services;
        }

        /// <summary>
        /// 添加 Serilog 配置
        /// </summary>
        /// <param name="builder"></param>
        public static WebApplicationBuilder AddSerilogConfiguration(this WebApplicationBuilder builder )
        {
            // 从配置文件中获取 Serilog 配置信息
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(
                new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
                .AddEnvironmentVariables()
                .Build())
                .CreateLogger()
                ;

            builder.Host.UseSerilog((context, service, logconfig) =>
            {
                logconfig.ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(service)
                .Enrich.FromLogContext();
            });


            return builder;
        }


    }
}
