using FluentAssertions;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using InprovePlan.UnitTests.TestDoubles;
using InprovePlan.UserCase.Behaviors;
using Instructure.Attributes;
using Instructure.Exceptions;
using Instructure.IResult;
using Instructure.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace InprovePlan.UnitTests.Behaviors;

/// <summary>
/// MediatR 授权管道测试。
///
/// 注意：
/// 这里假设 AuthorizationBehavior 使用 AnyAsync 进行非跟踪授权判断。
/// </summary>
public sealed class AuthorizationBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldSkipAuthorization_WhenRequestHasNoAttribute()
    {
        var currentUser = new FakeCurrentUser();
        var repository = new Mock<IReadRepository<AppUser>>();
        var logger = new Mock<ILogger<AuthorizationBehavior<PublicRequest, Result>>>();

        var behavior = new AuthorizationBehavior<PublicRequest, Result>(
            currentUser,
            logger.Object,
            repository.Object);

        var result = await behavior.Handle(
            new PublicRequest(),
            _ => Task.FromResult(Result.SeccessWithNoMsg),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        repository.Verify(
            x => x.FirstOrDefaultAsNoTrackingAsync(It.IsAny<Expression<Func<AppUser, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenCurrentUserIsMissing()
    {
        var currentUser = new FakeCurrentUser { Id = null };
        var repository = new Mock<IReadRepository<AppUser>>();
        var logger = new Mock<ILogger<AuthorizationBehavior<ProtectedRequest, Result>>>();

        var behavior = new AuthorizationBehavior<ProtectedRequest, Result>(
            currentUser,
            logger.Object,
            repository.Object);

        var action = async () => await behavior.Handle(
            new ProtectedRequest(),
            _ => Task.FromResult(Result.SeccessWithNoMsg),
            CancellationToken.None);

        await action.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowForbidden_WhenCurrentUserIsInvalid()
    {
        var currentUser = new FakeCurrentUser { Id = 1 };
        var repository = new Mock<IReadRepository<AppUser>>();

        repository
            .Setup(x => x.FirstOrDefaultAsNoTrackingAsync(It.IsAny<Expression<Func<AppUser, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser()
            {
                Id = 1,
                UserStatus = AppUserStatus.Enable,
                IsDeleted = true
            });

        var logger = new Mock<ILogger<AuthorizationBehavior<ProtectedRequest, Result>>>();

        var behavior = new AuthorizationBehavior<ProtectedRequest, Result>(
            currentUser,
            logger.Object,
            repository.Object);

        var action = async () => await behavior.Handle(
            new ProtectedRequest(),
            _ => Task.FromResult(Result.SeccessWithNoMsg),
            CancellationToken.None);

        await action.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task Handle_ShouldContinue_WhenCurrentUserIsValid()
    {
        var currentUser = new FakeCurrentUser { Id = 1 };
        var repository = new Mock<IReadRepository<AppUser>>();

        repository
            .Setup(x => x.FirstOrDefaultAsNoTrackingAsync(It.IsAny<Expression<Func<AppUser, bool>>>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new AppUser()
             {
                 Id = 1,
                 UserName = "test_user",
                 Email = "test@example.com",
                 PhoneNumber = "13900000000",
                 PasswordHash = "HASH",
                 UserStatus = AppUserStatus.Enable,
                 IsDeleted = false
             });

        var logger = new Mock<ILogger<AuthorizationBehavior<ProtectedRequest, Result>>>();

        var behavior = new AuthorizationBehavior<ProtectedRequest, Result>(
            currentUser,
            logger.Object,
            repository.Object);

        var result = await behavior.Handle(
            new ProtectedRequest(),
            _ => Task.FromResult(Result.SeccessWithNoMsg),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    

    public sealed record PublicRequest : ICommand<Result>;

    [RequireAuthorization]
    public sealed record ProtectedRequest : ICommand<Result>;
}