using FluentAssertions;
using InprovePlan.Data.Seeding;
using InprovePlan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InprovePlan.IntegrationTests.Infrastructure;

/// <summary>
/// Seeder 集成测试。
///
/// 覆盖之前出现过的问题：
/// - Id 重复
/// - Email 重复
/// - PhoneNumber 重复
/// - ProductCode 重复
/// - OrderNo 重复
/// - 多次执行幂等
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class AppDbContextDataSeederTests
{
    private readonly MySqlTestFixture _fixture;

    public AppDbContextDataSeederTests(MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SeedAsync_ShouldBeIdempotent()
    {
        await _fixture.ResetDatabaseAsync();

        var seeder = new AppDbContextDataSeeder(
            _fixture.DbContext,
            _fixture.PasswordHasher,
            _fixture.IdGenerator);

        await seeder.SeedAsync( TestContext.Current.CancellationToken);
        await seeder.SeedAsync(TestContext.Current.CancellationToken);

        var userCount = await _fixture.DbContext.Set<AppUser>().CountAsync(TestContext.Current.CancellationToken);
        var productCount = await _fixture.DbContext.Set<Product>().CountAsync( TestContext.Current.CancellationToken);
        var orderCount = await _fixture.DbContext.Set<AppOrder>().CountAsync(TestContext.Current.CancellationToken);

        userCount.Should().Be(50);
        productCount.Should().Be(100);
        orderCount.Should().Be(300);
    }
}