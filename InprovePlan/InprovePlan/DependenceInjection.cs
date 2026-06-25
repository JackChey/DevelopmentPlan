using InprovePlan.Connections;
using InprovePlan.Filters;
using InprovePlan.IService.Prometheus;
using InprovePlan.Prometheus;
using InprovePlan.Prometheus.AppMetrics;
using InprovePlan.Service;
using InprovePlan.Service.Prometheus;
using InprovePlan.UserCase;
using Instructure.Data;
using Instructure.Interceptors;
using Instructure.Interfaces;
using Instructure.Interfaces.Jwt;
using Instructure.Response;
using Instructure.Services.Jwt;
using Instructure.SystemLogs.LogEvents;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
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
           

            services.ConfigEnvironment(configuration);

            services.AddInfranstructureServices(configuration);

            services.ConfigDbContext(configuration);
            services.AddUserCaselService(configuration);

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
                options.Filters.Add<AppActionFilter>();
            });

            services.AddTransient<IJwtService, JwtService>();

            ConfigureAuthentication(services, jwtsettings);

            ConfigPrometheus(services);

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
                        // 这里加：401 计数
                        AppCustomMetrics.AuthAccessUnauthorizedTotal.Inc();

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
                        // 这里加：403 计数
                        AppCustomMetrics.AuthAccessForbiddenTotal.Inc();

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
            var dbConnectionStr = configuration.GetSection("ConnectionStrings:DBConnection");
            var rediaConnectionStr = configuration.GetSection("ConnectionStrings:RedisConnection");
            var rabbitMqConnectionStr = configuration.GetSection("ConnectionStrings:RabbitMqConnection");
            var prometheusSeetings = configuration.GetSection("PrometheusSettings");

            services.Configure<DBConnection>(dbConnectionStr);
            services.Configure<RedisConnection>(rediaConnectionStr);
            services.Configure<RabbitMqConnection>(rabbitMqConnectionStr);
            services.Configure<PrometheusSettings>(prometheusSeetings);

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

            //builder.Services.AddTransient<ILogEventSink, SerilogEventSink>();

            // 获取实例标识
            var instance = Environment.GetEnvironmentVariable("POD_NAME") ?? Environment.GetEnvironmentVariable("HOSTNAME") ?? $"{Environment.MachineName}-{Environment.ProcessId}";

            var appservice = Assembly.GetExecutingAssembly().GetName().Name ?? "No Service";
            var version = Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version;
            var env = builder.Environment.EnvironmentName;

            builder.Host.UseSerilog((context, service, logconfig) =>
            {
                //var sink = service.GetRequiredService<ILogEventSink>();

                logconfig.ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(service)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("instance", instance)
                .Enrich.WithProperty("service", appservice)
                .Enrich.WithProperty("version", version)
                .Enrich.WithProperty("env", env)
                .WriteTo.Sink(new LevelSeparatingSink(
                    highLevelPath: "Logs/AppExpLogs-.ndjson",
                    lowLevelPath: "Logs/AppLogs-.ndjson"))
                //.WriteTo.Sink(sink)
                ;
            });

            return builder;
        }

        /// <summary>
        /// 配置 Prometheus
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection ConfigPrometheus(this IServiceCollection services)
        {
            services.AddHttpClient("prom-check");

            services.AddHttpClient<IPrometheusQueryService, PrometheusQueryService>((sp, client) =>
            {
                var opt = sp.GetRequiredService<IOptions<PrometheusSettings>>().Value;

                if (string.IsNullOrWhiteSpace(opt.IP))
                    throw new InvalidOperationException("Prometheus:BaseUrl is not configured.");

                // Prometheus 服务地址（按实际环境改）
                client.BaseAddress = new Uri($"http://{opt.IP}:{opt.Port}");

                // 建议设置短超时，避免业务线程长时间等待监控系统
                client.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds <= 0 ? 5 : opt.TimeoutSeconds);
            });

            services.AddHostedService<PrometheusMetricStartupCheck>();

            return services;
        }

        /// <summary>
        /// 配置 数据库
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection ConfigDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            // 注册当前用户
            services.AddScoped<IUser, CurrentUser>();

            // 获取数据库链接字符串
            var connectionstring = configuration.GetConnectionString("AppDbConnectionStrings");

            // 注册拦截器 审计数据
            services.AddScoped<ISaveChangesInterceptor, AuditEntityInterceptor>();
            services.AddScoped<QueryCounterInterceptor>();

            services.AddTransient<IIdGenerator, SnowflakeIdGenerator>();

            // 配置数据库上下文
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                // 添加拦截器
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

                options.AddInterceptors(sp.GetRequiredService<QueryCounterInterceptor>());

                // 使用mysql作为数据库并自动检测版本
                options.UseMySql(connectionstring, ServerVersion.AutoDetect(connectionstring));
            });

            return services;
        }
    }
}
