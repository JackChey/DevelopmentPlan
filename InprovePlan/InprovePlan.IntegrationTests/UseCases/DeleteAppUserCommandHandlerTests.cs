using FluentAssertions;
using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.AppUsers.Commands;
using Instructure.Repositories;
using Xunit;

namespace InprovePlan.IntegrationTests.UseCases;

[Collection(Infrastructure.IntegrationTestCollection.Name)]
public sealed class DeleteAppUserCommandHandlerTests
{
    private readonly Infrastructure.MySqlTestFixture _fixture;

    public DeleteAppUserCommandHandlerTests(Infrastructure.MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteUser()
    {
        await _fixture.ResetDatabaseAsync();

        var user = Infrastructure.TestEntityFactory.CreateUser(
            _fixture.IdGenerator,
            _fixture.PasswordHasher,
            "delete_user",
            "delete@example.com",
            "13900000015");

        await _fixture.DbContext.Set<AppUser>().AddAsync(user, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeleteAppUserCommandHandler(
            new EfRepository<AppUser>(_fixture.DbContext));

        var result = await handler.Handle(
            new DeleteAppUserCommand(user.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.IsDeleted.Should().BeTrue();
        user.DeletedAt.Should().NotBeNull();
        user.UserStatus.Should().Be(AppUserStatus.Void);
    }
}