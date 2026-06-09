using FluentAssertions;
using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.AppUsers.Queries;
using Instructure.Paging;
using Instructure.Repositories;
using Instructure.Sorting;
using Xunit;

namespace InprovePlan.IntegrationTests.Infrastructure;

/// <summary>
/// 用户查询集成测试。
///
/// 覆盖：
/// - 单条查询
/// - 分页查询
/// - 关键字过滤
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class AppUserQueryTests
{
    private readonly MySqlTestFixture _fixture;

    public AppUserQueryTests(MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetById_ShouldReturnUser()
    {
        await _fixture.ResetDatabaseAsync();

        var user = TestEntityFactory.CreateUser(
            _fixture.IdGenerator,
            _fixture.PasswordHasher,
            "query_user",
            "query_user@example.com",
            "13900000002");

        await _fixture.DbContext.Set<AppUser>().AddAsync(user, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAppUserByIdQueryHandler(
            new EfReadRepository<AppUser>(_fixture.DbContext));

        var result = await handler.Handle(
            new GetAppUserByIdQuery(user.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetPaged_ShouldFilterByKeyword()
    {
        await _fixture.ResetDatabaseAsync();

        var users = new[]
        {
            TestEntityFactory.CreateUser(_fixture.IdGenerator, _fixture.PasswordHasher, "alice", "alice@example.com", "13900000003"),
            TestEntityFactory.CreateUser(_fixture.IdGenerator, _fixture.PasswordHasher, "bob", "bob@example.com", "13900000004")
        };

        await _fixture.DbContext.Set<AppUser>().AddRangeAsync(users);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAppUsersPagedQueryHandler(
            new EfReadRepository<AppUser>(_fixture.DbContext));

        var result = await handler.Handle(
            new GetAppUsersPagedQuery(
                new Pagination { PageIndex = 1, PageSize = 10 },
                new SortQuery { SortBy = "createdAt", SortDirection = "desc" },
                "alice",
                null,
                null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
        result.Value.Items.Single().UserName.Should().Be("alice");
    }
}