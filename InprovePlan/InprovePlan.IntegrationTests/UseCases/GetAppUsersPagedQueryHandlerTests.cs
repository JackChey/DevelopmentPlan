using FluentAssertions;
using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.AppUsers.Queries;
using Instructure.Paging;
using Instructure.Repositories;
using Instructure.Sorting;
using Xunit;

namespace InprovePlan.IntegrationTests.UseCases;

[Collection(Infrastructure.IntegrationTestCollection.Name)]
public sealed class GetAppUsersPagedQueryHandlerTests
{
    private readonly Infrastructure.MySqlTestFixture _fixture;

    public GetAppUsersPagedQueryHandlerTests(Infrastructure.MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_ShouldReturnPagedUsers()
    {
        await _fixture.ResetDatabaseAsync();

        var users = Enumerable.Range(1, 12)
            .Select(index => Infrastructure.TestEntityFactory.CreateUser(
                _fixture.IdGenerator,
                _fixture.PasswordHasher,
                $"page_user_{index:D4}",
                $"page_user_{index:D4}@example.com",
                $"138{index:D8}"))
            .ToList();

        await _fixture.DbContext.Set<AppUser>().AddRangeAsync(users, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAppUsersPagedQueryHandler(
            new EfReadRepository<AppUser>(_fixture.DbContext));

        var result = await handler.Handle(
            new GetAppUsersPagedQuery(
                new Pagination { PageIndex = 1, PageSize = 10 },
                new SortQuery { SortBy = "createdAt", SortDirection = "desc" },
                null,
                null,
                null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(12);
        result.Value.Count.Should().Be(10);
    }
}