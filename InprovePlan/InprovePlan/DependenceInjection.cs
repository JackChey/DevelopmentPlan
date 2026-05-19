using InprovePlan.Connections;
using InprovePlan.Filters;
using InprovePlan.IService.Jwt;
using InprovePlan.Service.Jwt;
using InprovePlan.SystemLogs.LogEvents;
using Instructure.Response;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Core;
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
            }, Assembly.GetExecutingAssembly());

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
        public static IServiceCollection AddInfranstructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 获取 Jwt设置
            var configurationSection = configuration.GetSection("JwtSettings");
            var jwtsettings = configurationSection.Get<JwtSettings>();

            if (jwtsettings is null)
            {
                throw new NullReferenceException(nameof(JwtSettings));
            }

            // 注入jwtsetting
            services.Configure<JwtSettings>(configurationSection);

            // 注册中间件Filter
            services.AddControllers(options =>
            {
                options.Filters.Add<AppAuthorizationFilter>();
                options.Filters.Add<AppActionFilter>();
            });

            services.AddTransient<IJwtService, JwtService>();

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

                option.Events = new JwtBearerEvents()
                {
                    // 处理身份检验失败
                    OnChallenge = async context =>
                    {
                        // 阻止 401 纯文本响应
                        context.HandleResponse();

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json; charset=utf-8";

                        var body = ApiResponse<object?>.Fail(
                            "unauthorized",
                            "Unauthorized",
                            context.HttpContext.TraceIdentifier);

                        await context.Response.WriteAsJsonAsync(body);
                    },

                    // 处理未授权
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json; charset=utf-8";

                        var body = ApiResponse<object?>.Fail(
                            "forbidden",
                            "Forbidden",
                            context.HttpContext.TraceIdentifier);

                        await context.Response.WriteAsJsonAsync(body);
                    },
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
        public static WebApplicationBuilder AddSerilogConfiguration(this WebApplicationBuilder builder)
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


            builder.Services.AddHttpContextAccessor();

            builder.Services.AddTransient<ILogEventSink, SerilogEventSink>();

            // 获取实例标识
            var instance = Environment.GetEnvironmentVariable("POD_NAME") ?? Environment.GetEnvironmentVariable("HOSTNAME") ?? $"{Environment.MachineName}-{Environment.ProcessId}";

            var appservice = Assembly.GetExecutingAssembly().GetName().Name ?? "No Service";
            var version = Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version;
            var env = builder.Environment.EnvironmentName;

            builder.Host.UseSerilog((context, service, logconfig) =>
            {
                var sink = service.GetRequiredService<ILogEventSink>();

                logconfig.ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(service)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("instance", instance)
                .Enrich.WithProperty("service", appservice)
                .Enrich.WithProperty("version", version)
                .Enrich.WithProperty("env", env)
                .WriteTo.Sink(sink)
                ;
            });




            return builder;
        }


    }
}
