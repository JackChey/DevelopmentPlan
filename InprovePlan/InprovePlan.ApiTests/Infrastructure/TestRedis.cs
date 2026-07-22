using DotNet.Testcontainers.Containers;
using InprovePlan.ApiTests.TestDoubles;
using Instructure.Caching;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.Redis;

namespace InprovePlan.ApiTests.Infrastructure;

public sealed class TestRedis : IAsyncDisposable
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7.0")
        .Build();

    private IConnectionMultiplexer _connection = default!;

    public IConnectionMultiplexer Connection => _connection;

    public CacheOptions CacheOptions { get; } = new()
    {
        AppName = "InprovePlan-Test",
        Environment = "test",
        KeyVersion = "v1",
        DefaultDurationSeconds = 300,
        NullValueDurationSeconds = 60,
        JitterMaxSeconds = 30
    };

    public IAppCache AppCache { get; private set; } = default!;

    public ICacheKeyBuilder CacheKeyBuilder { get; private set; } = default!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        if (_container.State != TestcontainersStates.Running)
        {
            throw new InvalidOperationException("Redis container failed to start.");
        }

        var options = new ConfigurationOptions
        {
            AllowAdmin = true,
            AbortOnConnectFail = false,
            ConnectRetry = 5,
            SyncTimeout = 5000
        };

        options.EndPoints.Add(ConnectionString);

        _connection = await ConnectionMultiplexer.ConnectAsync(options);

        var db = _connection.GetDatabase();
        await db.PingAsync();

        CacheKeyBuilder = new FakeCacheKeyBuilder(CacheOptions);
        AppCache = new RedisAppCacheForTest(ConnectionString, CacheOptions);
    }

    public async Task ResetAsync()
    {
        if (_connection is null || !_connection.IsConnected)
        {
            await ReconnectAsync();
        }

        var endpoint = _connection!.GetEndPoints().Single();
        var server = _connection.GetServer(endpoint);

        await server.FlushDatabaseAsync();

        if (AppCache is RedisAppCacheForTest redisCache)
        {
            await redisCache.ClearAsync();
        }
    }

    private async Task ReconnectAsync()
    {
        var options = new ConfigurationOptions
        {
            AllowAdmin = true,
            AbortOnConnectFail = false,
            ConnectRetry = 5,
            SyncTimeout = 5000
        };

        options.EndPoints.Add(ConnectionString);

        _connection = await ConnectionMultiplexer.ConnectAsync(options);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}

