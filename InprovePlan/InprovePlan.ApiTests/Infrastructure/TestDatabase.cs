using Instructure.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Respawn;
using Testcontainers.MySql;

namespace InprovePlan.ApiTests.Infrastructure;

public sealed class TestDatabase : IAsyncDisposable
{
    private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.4")
        .WithDatabase("inproveplan_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private MySqlConnection _connection = default!;
    private Respawner _respawner = default!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        _connection = new MySqlConnection(ConnectionString);
        await _connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.MySql
        });
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(ConnectionString, new MySqlServerVersion(new Version(8, 4, 0)) )
            .Options;

        return new AppDbContext(options);
    }

    public Task ResetAsync()
    {
        return _respawner.ResetAsync(_connection);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _container.DisposeAsync();
    }
}
