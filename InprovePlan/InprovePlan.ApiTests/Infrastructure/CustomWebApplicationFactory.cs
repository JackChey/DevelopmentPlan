using DotNet.Testcontainers.Containers;
using InprovePlan.ApiTests.TestDoubles;
using Instructure.Caching;
using Instructure.Data;
using Instructure.Interceptors;
using Instructure.Interfaces;
using Instructure.Interfaces.Jwt;
using Instructure.IResult;
using InprovePlan.ShareKernel.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MySqlConnector;
using Respawn;
using StackExchange.Redis;
using Testcontainers.MySql;
using Testcontainers.Redis;

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
                    ServerVersion.AutoDetect(_database.ConnectionString),
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

    }
}

