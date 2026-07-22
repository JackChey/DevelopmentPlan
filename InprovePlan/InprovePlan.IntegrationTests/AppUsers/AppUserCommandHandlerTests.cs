using InprovePlan.Domain.Entities;
using InprovePlan.IntegrationTests.Helpers;
using InprovePlan.IntegrationTests.Infrastructure;
using InprovePlan.IntegrationTests.TestDoubles;
using InprovePlan.UserCase.AppUsers.Commands;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.IntegrationTests.AppUsers;

/// <summary>
/// 应用用户命令处理器的集成测试类。
/// 该类专门用于测试涉及用户创建、更新等写操作的命令处理器。
/// 使用 [Collection] 特性确保测试在指定的 MySQL 集成测试集合中串行执行，
/// 以避免数据库状态竞争或并行执行导致的数据冲突。
/// </summary>
[Collection(MySqlIntegrationTestCollection.Name)]
public sealed class AppUserCommandHandlerTests
{
    // 测试夹具（Fixture），用于管理测试生命周期内的共享资源，如数据库连接、ID生成器、密码哈希器等
    private readonly MySqlTestFixture _fixture;

    /// <summary>
    /// 构造函数，通过依赖注入获取测试夹具实例。
    /// </summary>
    /// <param name="fixture">MySQL 测试环境配置实例</param>
    public AppUserCommandHandlerTests(MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// 测试场景：当用户名不存在时，执行创建用户命令。
    /// 预期结果：
    /// 1. 命令执行成功。
    /// 2. 返回的用户信息中，用户名和邮箱已自动去除首尾空格并转换为小写（标准化处理）。
    /// 3. 用户初始状态为“启用”（Enable）。
    /// 4. 数据库中确实持久化了该用户记录，且未被标记为删除。
    /// </summary>
    [Fact]
    public async Task CreateAppUser_WhenUserNameDoesNotExist_ShouldCreateUser()
    {
        // 1. 环境准备：重置数据库，确保测试在一个干净的状态下开始
        await _fixture.ResetDatabaseAsync();

        // 2. 数据库上下文初始化：创建一个新的 DbContext 实例用于本次测试
        await using var dbContext = _fixture.CreateDbContext();

        // 3. 依赖服务初始化：
        // EfRepository<AppUser>: 用户实体的读写仓库，用于持久化新用户
        var repository = new EfRepository<AppUser>(dbContext);

        // CreateMapper: 创建 AutoMapper 实例，用于命令对象到实体对象的映射
        // TestMapperFactory.Create(): 工厂方法，提供配置好的映射器实例
        // _fixture.PasswordHasher: 密码哈希服务，用于在保存前对明文密码进行加密处理
        var handler = new CreateAppUserCommandHandler(
            repository,
            TestMapperFactory.Create(),
            _fixture.PasswordHasher);

        // 4. 构建命令对象：
        // 注意输入数据包含空格和大写字母，用于验证处理器的数据清洗/标准化逻辑
        var command = new CreateAppUserCommand(
            UserName: " NewUser ",             // 包含前后空格
            Password: "Password123!",          // 明文密码
            Sex: AppUserSex.Secret,            // 性别：保密
            PhoneNumber: "13900000001",        // 手机号
            Email: "NEWUSER@EXAMPLE.COM");     // 全大写邮箱

        // 5. 执行命令：调用 Handle 方法处理创建用户请求
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // 6. 结果断言：
        // 验证命令执行状态为成功 (Ok)
        result.Status.Should().Be(ResultStatus.Ok);

        // 验证返回的用户对象不为空
        result.Value.Should().NotBeNull();

        // 验证用户名已去除空格并可能进行了标准化（此处期望为 "NewUser"，具体取决于业务规则，通常可能是 Trim 后保留大小写或转小写）
        // 根据断言 "NewUser"，推测业务逻辑是 Trim 空格但保留原始大小写，或者首字母大写其余小写等特定规则。
        // *修正观察*：结合 Email 断言为小写，UserName 断言为 "NewUser" (首字母大写)，说明 UserName 可能仅做了 Trim 或特定格式化。
        result.Value.UserName.Should().Be("NewUser");

        // 验证邮箱已转换为全小写，这是常见的数据标准化实践
        result.Value.Email.Should().Be("newuser@example.com");

        // 验证用户初始状态为“启用”
        result.Value.UserStatus.Should().Be(AppUserStatus.Enable);

        // 7. 数据库持久化验证：
        // 直接查询数据库，确认用户记录已存在，且未被软删除 (!IsDeleted)
        var exists = await repository.AnyAsync(
            user => user.UserName == "NewUser" && !user.IsDeleted,
            TestContext.Current.CancellationToken);

        // 断言数据库中确实存在该记录
        exists.Should().BeTrue();
    }

    /// <summary>
    /// 测试场景：当用户存在时，执行更新用户命令。
    /// 预期结果：
    /// 1. 命令执行成功。
    /// 2. 用户的姓名、邮箱、手机号、性别和状态均被正确更新。
    /// 3. 邮箱同样进行了小写标准化处理。
    /// </summary>
    [Fact]
    public async Task UpdateAppUser_WhenUserExists_ShouldUpdateUser()
    {
        // 1. 环境准备：重置数据库
        await _fixture.ResetDatabaseAsync();

        // 2. 数据库上下文初始化
        await using var dbContext = _fixture.CreateDbContext();

        // 3. 种子数据准备：
        // 创建一个初始用户实体
        var user = TestEntityFactory.CreateUser(_fixture.IdGenerator, _fixture.PasswordHasher);

        // 将用户添加到上下文并保存到数据库，模拟已存在的用户
        await dbContext.Set<AppUser>().AddAsync(user, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 4. 依赖服务初始化：
        // EfRepository<AppUser>: 用户读写仓库
        var repository = new EfRepository<AppUser>(dbContext);

        // UpdateAppUserCommandHandler: 更新用户命令处理器，仅注入仓库依赖
        var handler = new UpdateAppUserCommandHandler(repository);

        // 5. 构建更新命令：
        // 提供新的用户信息，包含空格和大写字母，用于验证更新时的数据清洗逻辑
        var command = new UpdateAppUserCommand(
            Id: user.Id,                       // 指定要更新的用户 ID
            UserName: " UpdatedUser ",         // 新用户名，含空格
            Email: "UPDATED@EXAMPLE.COM",      // 新邮箱，全大写
            PhoneNumber: "13900000002",        // 新手机号
            Sex: AppUserSex.Male,              // 新性别：男
            UserStatus: AppUserStatus.Frozen); // 新状态：冻结

        // 6. 执行命令：调用 Handle 方法处理更新用户请求
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // 7. 结果断言：
        // 验证命令执行状态为成功 (Ok)
        result.Status.Should().Be(ResultStatus.Ok);

        // 验证返回的用户对象不为空
        result.Value.Should().NotBeNull();

        // 验证用户名已更新并去除了空格
        result.Value.UserName.Should().Be("UpdatedUser");

        // 验证邮箱已更新并转换为小写
        result.Value.Email.Should().Be("updated@example.com");

        // 验证手机号已更新
        result.Value.PhoneNumber.Should().Be("13900000002");

        // 验证性别已更新为男性
        result.Value.Sex.Should().Be(AppUserSex.Male);

        // 验证用户状态已更新为冻结
        result.Value.UserStatus.Should().Be(AppUserStatus.Frozen);
    }

    /// <summary>
    /// 测试场景：当用户存在时，执行删除用户命令。
    /// 预期结果：
    /// 1. 命令执行成功。
    /// 2. 用户被软删除（IsDeleted 标记为 true），而非从数据库中物理移除。
    /// 3. 用户状态更新为“注销/无效”（Void）。
    /// 4. 删除时间（DeletedAt）被正确记录。
    /// </summary>
    [Fact]
    public async Task DeleteAppUser_WhenUserExists_ShouldSoftDeleteUser()
    {
        // 1. 环境准备：重置数据库，确保测试环境干净
        await _fixture.ResetDatabaseAsync();

        // 2. 数据库上下文初始化
        await using var dbContext = _fixture.CreateDbContext();

        // 3. 种子数据准备：创建一个测试用户并保存到数据库
        var user = TestEntityFactory.CreateUser(_fixture.IdGenerator, _fixture.PasswordHasher);
        await dbContext.Set<AppUser>().AddAsync(user, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 4. 依赖服务初始化：
        // EfRepository<AppUser>: 用户读写仓库，用于执行删除操作
        var repository = new EfRepository<AppUser>(dbContext);

        // DeleteAppUserCommandHandler: 删除用户命令处理器
        var handler = new DeleteAppUserCommandHandler(repository);

        // 5. 执行命令：传入要删除的用户 ID
        var result = await handler.Handle(
            new DeleteAppUserCommand(user.Id),
            TestContext.Current.CancellationToken);

        // 6. 结果断言：
        // 验证命令执行状态为成功 (Ok)
        result.Status.Should().Be(ResultStatus.Ok);

        // 验证用户实体已被标记为软删除
        user.IsDeleted.Should().BeTrue();

        // 验证用户状态已更新为 Void（注销/无效），表示该账户不再活跃
        user.UserStatus.Should().Be(AppUserStatus.Void);

        // 验证删除时间戳已被设置，不为空
        user.DeletedAt.Should().NotBeNull();
    }

    /// <summary>
    /// 测试场景：当旧密码正确时，执行修改密码命令。
    /// 预期结果：
    /// 1. 命令执行成功。
    /// 2. 新密码哈希值已更新，且能通过新密码验证。
    /// 3. 旧密码无法再通过验证，确保密码已真正变更。
    /// </summary>
    [Fact]
    public async Task ChangeAppUserPassword_WhenOldPasswordIsCorrect_ShouldUpdatePasswordHash()
    {
        // 1. 环境准备：重置数据库
        await _fixture.ResetDatabaseAsync();

        // 2. 数据库上下文初始化
        await using var dbContext = _fixture.CreateDbContext();

        // 3. 种子数据准备：
        // 创建一个具有特定初始密码 ("OldPassword123!") 的测试用户
        var user = TestEntityFactory.CreateUser(
            _fixture.IdGenerator,
            _fixture.PasswordHasher,
            password: "OldPassword123!");

        // 将用户保存到数据库
        await dbContext.Set<AppUser>().AddAsync(user, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 4. 依赖服务初始化：
        // EfRepository<AppUser>: 用户读写仓库
        var repository = new EfRepository<AppUser>(dbContext);

        // ChangeAppUserPasswordCommandHandler: 修改密码命令处理器，注入仓库和密码哈希器
        var handler = new ChangeAppUserPasswordCommandHandler(repository, _fixture.PasswordHasher);

        // 5. 构建修改密码命令：
        var command = new ChangeAppUserPasswordCommand(
            Id: user.Id,
            OldPassword: "OldPassword123!",      // 正确的旧密码
            NewPassword: "NewPassword123!",      // 新密码
            ConfirmPassword: "NewPassword123!"); // 确认新密码

        // 6. 执行命令
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // 7. 结果断言：
        // 验证命令执行状态为成功 (Ok)
        result.Status.Should().Be(ResultStatus.Ok);

        // 验证新密码哈希值是否正确：使用 PasswordHasher 验证新密码 "NewPassword123!" 应成功
        _fixture.PasswordHasher.Verify(user.PasswordHash, "NewPassword123!")
            .Should().Be(PasswordVerifyResult.Success);

        // 验证旧密码已失效：使用 PasswordHasher 验证旧密码 "OldPassword123!" 应失败
        // 这确保了数据库中的密码哈希确实已更新，而不是保持不变
        _fixture.PasswordHasher.Verify(user.PasswordHash, "OldPassword123!")
            .Should().Be(PasswordVerifyResult.Failed);
    }

    /// <summary>
    /// 测试场景：当密码正确时，执行用户登录命令。
    /// 预期结果：
    /// 1. 命令执行成功。
    /// 2. 返回有效的访问令牌（Access Token）。
    /// 3. 验证用户名输入时的空格修剪逻辑（" loginUser " -> "loginUser"）。
    /// </summary>
    [Fact]
    public async Task LoginAppUser_WhenPasswordIsCorrect_ShouldReturnAccessToken()
    {
        // 1. 环境准备：重置数据库
        await _fixture.ResetDatabaseAsync();

        // 2. 数据库上下文初始化
        await using var dbContext = _fixture.CreateDbContext();

        // 3. 种子数据准备：
        // 创建一个具有特定用户名 ("loginUser") 和密码 ("Password123!") 的测试用户
        var user = TestEntityFactory.CreateUser(
            _fixture.IdGenerator,
            _fixture.PasswordHasher,
            userName: "loginUser",
            password: "Password123!");

        // 将用户保存到数据库
        await dbContext.Set<AppUser>().AddAsync(user, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 4. 依赖服务初始化：
        // EfRepository<AppUser>: 用户读写仓库，用于查找用户
        var repository = new EfRepository<AppUser>(dbContext);

        // FakeJwtService: 模拟 JWT 服务，用于生成访问令牌。在测试中返回固定的令牌字符串以便断言
        var jwtService = new FakeJwtService { AccessToken = "access-token-for-test" };

        // LoginAppUserCommandHandler: 登录命令处理器，注入仓库、JWT服务和密码哈希器
        var handler = new LoginAppUserCommandHandler(repository, jwtService, _fixture.PasswordHasher);

        // 5. 构建登录命令：
        // 注意用户名包含前后空格 " loginUser "，用于验证处理器内部的 Trim 逻辑
        var result = await handler.Handle(
            new LoginAppUserCommand(" loginUser ", "Password123!"),
            TestContext.Current.CancellationToken);

        // 6. 结果断言：
        // 验证命令执行状态为成功 (Ok)
        result.Status.Should().Be(ResultStatus.Ok);

        // 验证返回的结果值不为空
        result.Value.Should().NotBeNull();

        // 验证返回的访问令牌与 FakeJwtService 中设置的预期令牌一致
        result.Value.AccessToken.Should().Be("access-token-for-test");
    }

}

