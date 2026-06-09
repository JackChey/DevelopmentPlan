using Instructure.Data;
using Instructure.Interfaces;
using Instructure.Interfaces.Jwt;
using Instructure.IResult;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using MySqlConnector;
using Respawn;
using Testcontainers.MySql;
using Xunit;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Instructure.Interceptors;

namespace InprovePlan.ApiTests.Infrastructure;

/// <summary>
/// API 测试宿主工厂。
///
/// 作用：
/// 1. 启动真实 MySQL 测试容器。
/// 2. 替换应用中的 AppDbContext。
/// 3. 执行 EF Core 迁移。
/// 4. 替换测试所需的基础服务。
/// 5. 提供数据库重置能力。
/// </summary>
public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MySqlContainer _container = new MySqlBuilder()
        .WithImage("mysql:8.4")
        .WithDatabase("inproveplan_api_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private MySqlConnection _connection = default!;

    private Respawner _respawner = default!;

    public ApiTestCurrentUser CurrentUser { get; } = new();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        _connection = new MySqlConnection(ConnectionString);
        await _connection.OpenAsync();

        using var scope = Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.MySql
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        //builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            services.RemoveAll<IIdGenerator>();
            services.RemoveAll<IPasswordHasher>();
            services.RemoveAll<IJwtService>();
            services.RemoveAll<IUser>();

            services.AddSingleton<IIdGenerator, ApiTestIdGenerator>();
            services.AddSingleton<IPasswordHasher, ApiTestPasswordHasher>();
            services.AddSingleton<IJwtService, ApiTestJwtService>();
            services.AddSingleton<IUser>(CurrentUser);

            // 如果原来的拦截器注册被移除了或依赖被替换，建议显式补上
            services.RemoveAll<ISaveChangesInterceptor>();
            services.AddScoped<ISaveChangesInterceptor, AuditEntityInterceptor>();

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

                options.UseMySql(
                    ConnectionString,
                    ServerVersion.AutoDetect(ConnectionString),
                    mysqlOptions =>
                    {
                        mysqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    });
            });
        });
    }

    /// <summary>
    /// 清理测试数据库。
    ///
    /// 建议每个 API 测试开始前调用。
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_connection);
    }

    public new async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _container.DisposeAsync();

        await base.DisposeAsync();
    }
}