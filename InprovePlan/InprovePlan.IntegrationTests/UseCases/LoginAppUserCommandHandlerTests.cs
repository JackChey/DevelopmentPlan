using FluentAssertions;
using InprovePlan.Domain.Entities;
using InprovePlan.IntegrationTests.TestDoubles;
using InprovePlan.UserCase.AppUsers.Commands;
using Instructure.Repositories;
using Xunit;

namespace InprovePlan.IntegrationTests.UseCases;

[Collection(Infrastructure.IntegrationTestCollection.Name)]
public sealed class LoginAppUserCommandHandlerTests
{
    private readonly Infrastructure.MySqlTestFixture _fixture;

    public LoginAppUserCommandHandlerTests(Infrastructure.MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_ShouldReturnToken_WhenPasswordIsCorrect()
    {
        await _fixture.ResetDatabaseAsync();

        var user = Infrastructure.TestEntityFactory.CreateUser(
            _fixture.IdGenerator,
            _fixture.PasswordHasher,
            "login_user",
            "login_user@example.com",
            "13900000012");

        await _fixture.DbContext.Set<AppUser>().AddAsync(user, TestContext.Current.CancellationToken);
        await _fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new LoginAppUserCommandHandler(
            new EfRepository<AppUser>(_fixture.DbContext),
            new FakeJwtService(),
            _fixture.PasswordHasher);

        var result = await handler.Handle(
            new LoginAppUserCommand("login_user", "Password123?"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be($"test-token-{user.Id}");
    }
}