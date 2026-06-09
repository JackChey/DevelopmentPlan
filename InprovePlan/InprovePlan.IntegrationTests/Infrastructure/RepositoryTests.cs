using FluentAssertions;
using InprovePlan.Domain.Entities;
using InprovePlan.IntegrationTests.Infrastructure;
using InprovePlan.UserCase.AppUsers.Queries;
using Instructure.Paging;
using Instructure.Repositories;
using Instructure.Sorting;
using Instructure.Sorting.SortWhitelists;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InprovePlan.IntegrationTests.Infrastructure;

/// <summary>
/// 仓储集成测试。
///
/// 验证仓储在真实 MySQL 下的基础行为。
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class RepositoryTests
{
    private readonly MySqlTestFixture _fixture;

    public RepositoryTests(MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_ShouldPersistEntity()
    {
        await _fixture.ResetDatabaseAsync();

        var repository = new EfRepository<AppUser>(_fixture.DbContext);

        var user = TestEntityFactory.CreateUser(
            _fixture.IdGenerator,
            _fixture.PasswordHasher,
            "repo_user",
            "repo_user@example.com",
            "13900000001");

        await repository.AddAsync(user, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        var exists = await _fixture.DbContext.Set<AppUser>()
            .AnyAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task PageAsync_ShouldReturnPagedResult()
    {
        await _fixture.ResetDatabaseAsync();

        var users = Enumerable.Range(1, 25)
            .Select(index => TestEntityFactory.CreateUser(
                _fixture.IdGenerator,
                _fixture.PasswordHasher,
                $"repo_user_{index:D4}",
                $"repo_user_{index:D4}@example.com",
                $"139{index:D8}"))
            .ToList();

        await _fixture.DbContext.Set<AppUser>().AddRangeAsync(users, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EfReadRepository<AppUser>(_fixture.DbContext);

        var query = new GetAppUsersPagedQuery(
            new Pagination { PageIndex = 1, PageSize = 10 },
            new SortQuery { SortBy = "createdAt", SortDirection = "desc" },
            null,
            null,
            null);

        var result = await repository.PageAsync(
            new AppUsersPagedSpecification(query),
            query.Pagination,
            query.Sort,
            AppUserSortWhitelist.Instance,
            TestContext.Current.CancellationToken);

        result.Total.Should().Be(25);
        result.Count.Should().Be(10);
        result.Items.Should().HaveCount(10);
    }
}