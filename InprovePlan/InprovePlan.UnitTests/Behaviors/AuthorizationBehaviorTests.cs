using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using InprovePlan.UnitTests.TestDoubles;
using InprovePlan.UserCase.Behaviors;
using Instructure.Attributes;
using Instructure.Exceptions;
using Instructure.IResult;
using Instructure.Repositories;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace InprovePlan.UnitTests.Behaviors;

using FluentAssertions;
using Moq;
using Xunit;

/// <summary>
/// 授权行为中间件 (AuthorizationBehavior) 的单元测试。
/// 主要覆盖以下核心场景：
/// 1. 无 [RequireAuthorization] 特性时，跳过授权检查。
/// 2. 当前用户未登录（Id为空）时，抛出未授权异常。
/// 3. 当前用户存在但状态无效（如已删除）时，抛出禁止访问异常。
/// 4. 当前用户有效时，正常继续执行后续管道。
/// </summary>
public sealed class AuthorizationBehaviorTests
{
    /// <summary>
    /// 测试场景：请求命令未标记 [RequireAuthorization] 特性。
    /// 预期结果：跳过授权逻辑，直接执行后续管道，且不查询数据库。
    /// 目的：确保公共接口（Public API）不会受到不必要的性能损耗或权限拦截。
    /// </summary>
    [Fact]
    public async Task Handle_ShouldSkipAuthorization_WhenRequestHasNoAttribute()
    {
        // --- Arrange (准备阶段) ---
        // 1. 模拟当前用户：创建一个空的 FakeCurrentUser，代表未特定化的上下文。
        var currentUser = new FakeCurrentUser();

        // 2. 模拟仓储：创建 IReadRepository<AppUser> 的 Mock 对象。
        var repository = new Mock<IReadRepository<AppUser>>();

        // 3. 模拟日志记录器：创建 ILogger 的 Mock 对象。
        var logger = new Mock<ILogger<AuthorizationBehavior<PublicRequest, Result>>>();

        // 4. 实例化待测行为中间件：
        // 使用 PublicRequest（无特性）作为泛型参数 TRequest。
        var behavior = new AuthorizationBehavior<PublicRequest, Result>(
            currentUser,
            logger.Object,
            repository.Object);

        // --- Act (执行阶段) ---
        // 执行 Handle 方法：
        // 传入 PublicRequest 实例和一个返回成功结果的委托（代表后续管道）。
        var result = await behavior.Handle(
            new PublicRequest(),
            _ => Task.FromResult(Result.SeccessWithNoMsg), // 模拟后续管道直接返回成功
            CancellationToken.None);

        // --- Assert (断言阶段) ---
        // 1. 断言最终结果为成功，说明中间件没有阻断流程。
        result.IsSuccess.Should().BeTrue();

        // 2. 断言仓储方法从未被调用：
        // 验证 FirstOrDefaultAsNoTrackingAsync 执行次数为 0 (Times.Never)。
        // 这证明了当请求没有 [RequireAuthorization] 特性时，中间件确实跳过了数据库查询步骤。
        repository.Verify(
            x => x.FirstOrDefaultAsNoTrackingAsync(It.IsAny<Expression<Func<AppUser, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 测试场景：请求命令标记了 [RequireAuthorization特性，但当前用户未登录（Id 为 null）。
    /// 预期结果：抛出 AuthorizationException 异常。
    /// 目的：确保受保护的接口必须要求用户处于登录状态。
    /// </summary>
    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenCurrentUserIsMissing()
    {
        // --- Arrange (准备阶段) ---
        // 1. 模拟当前用户：设置 Id 为 null，代表匿名用户或未通过身份认证的用户。
        var currentUser = new FakeCurrentUser { Id = null };

        // 2. 模拟仓储和日志记录器。
        var repository = new Mock<IReadRepository<AppUser>>();
        var logger = new Mock<ILogger<AuthorizationBehavior<ProtectedRequest, Result>>>();

        // 3. 实例化待测行为中间件：
        // 使用 ProtectedRequest（有 [RequireAuthorization] 特性）作为泛型参数 TRequest。
        var behavior = new AuthorizationBehavior<ProtectedRequest, Result>(
            currentUser,
            logger.Object,
            repository.Object);

        // --- Act (执行阶段) ---
        // 定义一个异步动作委托，用于捕获可能抛出的异常。
        var action = async () => await behavior.Handle(
            new ProtectedRequest(),
            _ => Task.FromResult(Result.SeccessWithNoMsg),
            CancellationToken.None);

        // --- Assert (断言阶段) ---
        // 断言：执行该动作应抛出 AuthorizationException。
        // 这表明中间件正确识别了“未登录”状态并拒绝了访问。
        await action.Should().ThrowAsync<AuthorizationException>();
    }

    /// <summary>
    /// 测试场景：当前用户已登录（Id 存在），但在数据库中查询到的用户状态无效（如 IsDeleted = true）。
    /// 预期结果：抛出 AuthorizationException 异常。
    /// 目的：确保已被逻辑删除或禁用的账户无法访问受保护资源。
    /// </summary>
    [Fact]
    public async Task Handle_ShouldThrowForbidden_WhenCurrentUserIsInvalid()
    {
        // --- Arrange (准备阶段) ---
        // 1. 模拟当前用户：设置 Id = 1，代表一个已登录的用户。
        var currentUser = new FakeCurrentUser { Id = 1 };

        // 2. 模拟仓储：
        // 设置 FirstOrDefaultAsNoTrackingAsync 的返回值。
        // 返回一个 IsDeleted = true 的 AppUser 对象，模拟该用户在数据库中已被删除。
        var repository = new Mock<IReadRepository<AppUser>>();
        repository
            .Setup(x => x.FirstOrDefaultAsNoTrackingAsync(It.IsAny<Expression<Func<AppUser, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser()
            {
                Id = 1,
                UserStatus = AppUserStatus.Enable, // 即使状态是 Enable，只要 IsDeleted 为 true 也应视为无效
                IsDeleted = true
            });

        var logger = new Mock<ILogger<AuthorizationBehavior<ProtectedRequest, Result>>>();

        // 3. 实例化待测行为中间件。
        var behavior = new AuthorizationBehavior<ProtectedRequest, Result>(
            currentUser,
            logger.Object,
            repository.Object);

        // --- Act (执行阶段) ---
        var action = async () => await behavior.Handle(
            new ProtectedRequest(),
            _ => Task.FromResult(Result.SeccessWithNoMsg),
            CancellationToken.None);

        // --- Assert (断言阶段) ---
        // 断言：执行该动作应抛出 AuthorizationException。
        // 这表明中间件在查询数据库后，正确识别了用户状态的非法性并拒绝了访问。
        await action.Should().ThrowAsync<AuthorizationException>();
    }

    /// <summary>
    /// 测试场景：当前用户已登录，且在数据库中查询到的用户状态完全有效。
    /// 预期结果：验证通过，继续执行后续管道，返回成功结果。
    /// 目的：确保合法用户可以正常访问受保护资源。
    /// </summary>
    [Fact]
    public async Task Handle_ShouldContinue_WhenCurrentUserIsValid()
    {
        // --- Arrange (准备阶段) ---
        // 1. 模拟当前用户：设置 Id = 1。
        var currentUser = new FakeCurrentUser { Id = 1 };

        // 2. 模拟仓储：
        // 设置 FirstOrDefaultAsNoTrackingAsync 的返回值。
        // 返回一个完整且有效的 AppUser 对象（IsDeleted = false, Status = Enable）。
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

        // 3. 实例化待测行为中间件。
        var behavior = new AuthorizationBehavior<ProtectedRequest, Result>(
            currentUser,
            logger.Object,
            repository.Object);

        // --- Act (执行阶段) ---
        // 执行 Handle 方法。
        var result = await behavior.Handle(
            new ProtectedRequest(),
            _ => Task.FromResult(Result.SeccessWithNoMsg), // 模拟后续管道返回成功
            CancellationToken.None);

        // --- Assert (断言阶段) ---
        // 断言：最终结果为成功。
        // 这表明中间件通过了所有校验，并将控制权顺利移交给了后续的业务逻辑。
        result.IsSuccess.Should().BeTrue();
    }

    // --- 辅助定义 ---

    /// <summary>
    /// 公共请求命令：未标记任何授权特性，用于测试“跳过授权”场景。
    /// </summary>
    public sealed record PublicRequest : ICommand<Result>;

    /// <summary>
    /// 受保护请求命令：标记了 [RequireAuthorization] 特性，用于测试“强制授权”场景。
    /// </summary>
    [RequireAuthorization]
    public sealed record ProtectedRequest : ICommand<Result>;
}
