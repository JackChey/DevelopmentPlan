using FluentAssertions;
using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.AppUsers.Commands;
using Instructure.Repositories;
using Xunit;

namespace InprovePlan.IntegrationTests.UseCases;

[Collection(Infrastructure.IntegrationTestCollection.Name)]
public sealed class UpdateAppUserCommandHandlerTests
{
    private readonly Infrastructure.MySqlTestFixture _fixture;

    public UpdateAppUserCommandHandlerTests(Infrastructure.MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_ShouldUpdateUser()
    {
        await _fixture.ResetDatabaseAsync();

        var user = Infrastructure.TestEntityFactory.CreateUser(
            _fixture.IdGenerator,
            _fixture.PasswordHasher,
            "old_name",
            "old@example.com",
            "13900000013");

        await _fixture.DbContext.Set<AppUser>().AddAsync(user, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateAppUserCommandHandler(
            new EfRepository<AppUser>(_fixture.DbContext));

        var result = await handler.Handle(
            new UpdateAppUserCommand(
                user.Id,
                "new_name",
                "new@example.com",
                "13900000014",
                AppUserSex.Male,
                AppUserStatus.Enable),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        user.UserName.Should().Be("new_name");
        user.Email.Should().Be("new@example.com");
    }
}