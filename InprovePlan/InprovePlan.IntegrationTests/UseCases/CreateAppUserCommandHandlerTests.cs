using AutoMapper;
using FluentAssertions;
using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.AppUsers;
using InprovePlan.UserCase.AppUsers.Commands;
using Instructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InprovePlan.IntegrationTests.UseCases;

[Collection(Infrastructure.IntegrationTestCollection.Name)]
public sealed class CreateAppUserCommandHandlerTests
{
    private readonly Infrastructure.MySqlTestFixture _fixture;

    public CreateAppUserCommandHandlerTests(Infrastructure.MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_ShouldCreateUser()
    {
        await _fixture.ResetDatabaseAsync();

        var mapper = new Mock<IMapper>();
        mapper.Setup(x => x.Map<AppUserDto>(It.IsAny<AppUser>()))
            .Returns<AppUser>(user => new AppUserDto(
                user.Id,
                user.UserName,
                user.Email,
                user.PhoneNumber,
                user.Sex,
                user.UserStatus));

        var handler = new CreateAppUserCommandHandler(
            new EfRepository<AppUser>(_fixture.DbContext),
            mapper.Object,
            _fixture.PasswordHasher);

        var result = await handler.Handle(
            new CreateAppUserCommand(
                "create_user",
                "Password123?",
                AppUserSex.Secret,
                "13900000011",
                "create_user@example.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var user = await _fixture.DbContext.Set<AppUser>()
            .SingleAsync(x => x.UserName == "create_user", TestContext.Current.CancellationToken);

        user.PasswordHash.Should().Be("HASH::Password123?");
        user.IsDeleted.Should().BeFalse();
    }
}