using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InprovePlan.ApiTests.Infrastructure;
using InprovePlan.Domain.Entities;
using Instructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InprovePlan.ApiTests;

/// <summary>
/// IdentityController API 测试。
///
/// 覆盖当前 Controller 中实际存在的接口：
/// - POST /api/Identity
///
/// 该接口用于用户登录。
/// </summary>
public sealed class IdentityControllerTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public IdentityControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ShouldReturnAccessToken_WhenUserNameAndPasswordAreCorrect()
    {
        await ResetAsync();

        var client = _factory.CreateClient();

        var createdUser = await CreateUserAsync(
            client,
            userName: "login_user",
            password: "Password123?",
            phoneNumber: "13900000201",
            email: "login_user@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/Identity",
            new
            {
                UserName = "login_user",
                Password = "Password123?"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<LoginAppUserDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.AccessToken.Should().Be($"api-test-token-{createdUser.Id}");
    }

    [Fact]
    public async Task Login_ShouldReturnFailure_WhenPasswordIsWrong()
    {
        await ResetAsync();

        var client = _factory.CreateClient();

        await CreateUserAsync(
            client,
            userName: "wrong_password_user",
            password: "Password123?",
            phoneNumber: "13900000202",
            email: "wrong_password_user@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/Identity",
            new
            {
                UserName = "wrong_password_user",
                Password = "WrongPassword123?"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<object>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Error.Should().NotBeNull();
        body.Error!.Details.Should().Contain(detail => detail.Contains("用户名或密码错误"));
    }

    [Fact]
    public async Task Login_ShouldReturnFailure_WhenUserDoesNotExist()
    {
        await ResetAsync();

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/Identity",
            new
            {
                UserName = "not_exists_user",
                Password = "Password123?"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<object>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Error.Should().NotBeNull();
        body.Error!.Details.Should().Contain(detail => detail.Contains("用户名或密码错误"));
    }

    [Fact]
    public async Task Login_ShouldReturnForbidden_WhenUserIsFrozen()
    {
        await ResetAsync();

        var client = _factory.CreateClient();

        var createdUser = await CreateUserAsync(
            client,
            userName: "frozen_user",
            password: "Password123?",
            phoneNumber: "13900000203",
            email: "frozen_user@example.com");

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = dbContext.Set<AppUser>().Single(user => user.Id == createdUser.Id);

            user.UserStatus = AppUserStatus.Frozen;

            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await client.PostAsJsonAsync(
            "/api/Identity",
            new
            {
                UserName = "frozen_user",
                Password = "Password123?"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<object>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Error.Should().NotBeNull();
    }

    /// <summary>
    /// 重置测试环境。
    ///
    /// 登录接口是匿名接口，不依赖 CurrentUser。
    /// 但这里仍重置 CurrentUser，避免其他测试污染。
    /// </summary>
    private async Task ResetAsync()
    {
        _factory.CurrentUser.Id = null;
        await _factory.ResetDatabaseAsync();
    }

    /// <summary>
    /// 通过真实 AppUser 注册接口创建用户。
    ///
    /// 这样可以保证测试用户密码哈希、默认状态等逻辑
    /// 与真实业务流程保持一致。
    /// </summary>
    private static async Task<AppUserDtoJson> CreateUserAsync(
        HttpClient client,
        string userName,
        string password,
        string phoneNumber,
        string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/AppUser",
            new
            {
                UserName = userName,
                Password = password,
                Sex = 2,
                PhoneNumber = phoneNumber,
                Email = email
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppUserDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();

        return body.Data!;
    }

    private sealed class ApiResponseJson<T>
    {
        public bool Success { get; set; }

        public T? Data { get; set; }

        public ApiErrorJson? Error { get; set; }

        public string TraceId { get; set; } = string.Empty;
    }

    private sealed class ApiErrorJson
    {
        public string Code { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string[] Details { get; set; } = [];
    }

    private sealed class AppUserDtoJson
    {
        public long Id { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public int Sex { get; set; }

        public int UserStatus { get; set; }
    }

    private sealed class LoginAppUserDtoJson
    {
        public string AccessToken { get; set; } = string.Empty;
    }
}