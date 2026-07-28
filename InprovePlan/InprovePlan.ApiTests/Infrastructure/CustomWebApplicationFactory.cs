using InprovePlan.ApiTests.TestDoubles;
using InprovePlan.ShareKernel.Messaging;
using Instructure.Caching;
using Instructure.Data;
using Instructure.Interceptors;
using Instructure.Interfaces;
using Instructure.Interfaces.Jwt;
using Instructure.IResult;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace InprovePlan.ApiTests.Infrastructure;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly TestDatabase _database = new();
    private readonly TestRedis _redis = new();

    public FakeCurrentUser CurrentUser { get; } = new();

    public IIdGenerator IdGenerator { get; } = new FakeIdGenerator();

    public IPasswordHasher PasswordHasher { get; } = new FakePasswordHasher();

    public async ValueTask InitializeAsync()
    {
        await _database.InitializeAsync();
        await _redis.InitializeAsync();

        SetTestEnvironmentVariables();
    }

    public async Task ResetDatabaseAsync()
    {
        CurrentUser.Id = null;

        await _database.ResetAsync();
        await _redis.ResetAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            services.RemoveAll<IIdGenerator>();
            services.RemoveAll<IPasswordHasher>();
            services.RemoveAll<IJwtService>();
            services.RemoveAll<IUser>();
            services.RemoveAll<IOrderEventPublisher>();
            services.RemoveAll<IConnectionMultiplexer>();

            services.RemoveAll<ICacheKeyBuilder>();
            services.RemoveAll<IAppCache>();

            services.AddSingleton(IdGenerator);
            services.AddSingleton(PasswordHasher);
            services.AddSingleton<IJwtService, FakeJwtService>();
            services.AddSingleton<IUser>(CurrentUser);
            services.AddSingleton<IOrderEventPublisher, FakeOrderEventPublisher>();
            services.AddSingleton(_redis.Connection);

            services.AddSingleton(_redis.CacheKeyBuilder);
            services.AddSingleton(_redis.AppCache);

            services.RemoveAll<ISaveChangesInterceptor>();
            services.AddScoped<ISaveChangesInterceptor, AuditEntityInterceptor>();

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

                options.UseMySql(
                    _database.ConnectionString,
                    //ServerVersion.AutoDetect(_database.ConnectionString),
                    new MySqlServerVersion(new Version(8, 4, 0)),
                    mysqlOptions =>
                    {
                        mysqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    });
            });
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        await _redis.DisposeAsync();
        await _database.DisposeAsync();

        ClearTestEnvironmentVariables();

    }

    private void SetTestEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__AppDbConnectionStrings",
            _database.ConnectionString);

        Environment.SetEnvironmentVariable(
            "ConnectionStrings__RedisConnection",
            _redis.ConnectionString);

        Environment.SetEnvironmentVariable("RabbitMq__Host", "localhost");
        Environment.SetEnvironmentVariable("RabbitMq__Port", "5672");
        Environment.SetEnvironmentVariable("RabbitMq__VirtualHost", "/");
        Environment.SetEnvironmentVariable("RabbitMq__Username", "guest");
        Environment.SetEnvironmentVariable("RabbitMq__Password", "guest");

        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "InprovePlan.Tests");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "InprovePlan.Tests");
        Environment.SetEnvironmentVariable(
            "JwtSettings__Secret",
            "test-secret-key-for-api-tests-at-least-32-bytes");

        Environment.SetEnvironmentVariable("JwtSettings__AccessTokenExpirationMinutes", "30");

        Environment.SetEnvironmentVariable("PrometheusSettings__IP", "localhost");
        Environment.SetEnvironmentVariable("PrometheusSettings__Port", "9090");
        Environment.SetEnvironmentVariable("PrometheusSettings__TimeoutSeconds", "5");
    }

    private static void ClearTestEnvironmentVariables()
    {
        foreach (var key in new[]
        {
        "ConnectionStrings__AppDbConnectionStrings",
        "ConnectionStrings__RedisConnection",
        "RabbitMq__Host",
        "RabbitMq__Port",
        "RabbitMq__VirtualHost",
        "RabbitMq__Username",
        "RabbitMq__Password",
        "JwtSettings__Issuer",
        "JwtSettings__Audience",
        "JwtSettings__Secret",
        "JwtSettings__AccessTokenExpirationMinutes",
        "PrometheusSettings__IP",
        "PrometheusSettings__Port",
        "PrometheusSettings__TimeoutSeconds"
    })
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }
}

