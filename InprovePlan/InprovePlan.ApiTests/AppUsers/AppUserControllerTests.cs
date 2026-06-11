using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InprovePlan.ApiTests.Infrastructure;
using InprovePlan.Domain.Entities;
using Instructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InprovePlan.ApiTests.AppUsers;

/// <summary>
/// AppUserController API 测试。
///
/// 覆盖当前 Controller 中实际存在的接口：
/// - POST   /api/AppUser
/// - GET    /api/AppUser/{id}
/// - GET    /api/AppUser
/// - PUT    /api/AppUser/{id}
/// - PUT    /api/AppUser/{id}/password
/// - DELETE /api/AppUser/{id}
///
/// 注意：
/// 当前 AppUserController 没有 Login 接口，
/// 所以登录 API 测试不放在这里。
/// </summary>
public sealed class AppUserControllerTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AppUserControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ShouldReturnSuccessResultFullResponse()
    {
        await ResetAsync();

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/AppUser",
            new
            {
                UserName = "api_user",
                Password = "Password123?",
                Sex = 2,
                PhoneNumber = "13900000099",
                Email = "api_user@example.com"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppUserDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.UserName.Should().Be("api_user");
        body.Data.Email.Should().Be("api_user@example.com");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = dbContext.Set<AppUser>().Single(user => user.UserName == "api_user");

        user.PasswordHash.Should().Be("HASH::Password123?");
        user.IsDeleted.Should().BeFalse();
        user.UserStatus.Should().Be(AppUserStatus.Enable);
    }

    [Fact]
    public async Task Create_ShouldReturnFailureResultFullResponse_WhenUserNameAlreadyExists()
    {
        await ResetAsync();

        var client = _factory.CreateClient();

        var request = new
        {
            UserName = "duplicate_user",
            Password = "Password123?",
            Sex = 2,
            PhoneNumber = "13900000100",
            Email = "duplicate_user@example.com"
        };

        var firstResponse = await client.PostAsJsonAsync(
            "/api/AppUser",
            request,
            TestContext.Current.CancellationToken);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResponse = await client.PostAsJsonAsync(
            "/api/AppUser",
            request,
            TestContext.Current.CancellationToken);

        secondResponse.StatusCode.Should().NotBe(HttpStatusCode.OK);

        var body = await secondResponse.Content.ReadFromJsonAsync<ApiResponseJson<object>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Error.Should().NotBeNull();
        body.Error!.Details.Should().Contain(detail => detail.Contains("用户名已存在"));
    }

    [Fact]
    public async Task GetById_ShouldReturnUser_WhenUserExists()
    {
        await ResetAsync();

        var client = _factory.CreateClient();

        var createdUser = await CreateUserAsync(
            client,
            userName: "get_user",
            password: "Password123?",
            phoneNumber: "13900000101",
            email: "get_user@example.com");

        _factory.CurrentUser.Id = createdUser.Id;

        var response = await client.GetAsync(
            $"/api/AppUser/{createdUser.Id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppUserDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Id.Should().Be(createdUser.Id);
        body.Data.UserName.Should().Be("get_user");
    }

    //[Fact]
    //public async Task GetPaged_ShouldReturnPagedUsers()
    //{
    //    await ResetAsync();

    //    var client = _factory.CreateClient();

    //    var firstUser = await CreateUserAsync(
    //        client,
    //        userName: "page_user_01",
    //        password: "Password123?",
    //        phoneNumber: "13900000102",
    //        email: "page_user_01@example.com");

    //    await CreateUserAsync(
    //        client,
    //        userName: "page_user_02",
    //        password: "Password123?",
    //        phoneNumber: "13900000103",
    //        email: "page_user_02@example.com");

    //    _factory.CurrentUser.Id = firstUser.Id;

    //    var response = await client.GetAsync(
    //        "/api/AppUser?pageIndex=1&pageSize=10&sortBy=createdAt&sortDirection=desc&keyword=page_user",
    //        TestContext.Current.CancellationToken);

    //    response.StatusCode.Should().Be(HttpStatusCode.OK);

    //    var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<PagedResultJson<AppUserDtoJson>>>(
    //        cancellationToken: TestContext.Current.CancellationToken);

    //    body.Should().NotBeNull();
    //    body!.Success.Should().BeTrue();
    //    body.Data.Should().NotBeNull();
    //    body.Data!.Total.Should().Be(2);
    //    body.Data.Count.Should().Be(2);
    //    body.Data.Items.Should().HaveCount(2);
    //}

    [Fact]
    public async Task GetPaged_ShouldReturnPagedUsers()
    {
        await ResetAsync();

        var client = _factory.CreateClient();

        var firstUser = await CreateUserAsync(
            client,
            userName: "page_user_01",
            password: "Password123?",
            phoneNumber: "13900000102",
            email: "page_user_01@example.com");

        await CreateUserAsync(
            client,
            userName: "page_user_02",
            password: "Password123?",
            phoneNumber: "13900000103",
            email: "page_user_02@example.com");

        _factory.CurrentUser.Id = firstUser.Id;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var exists = dbContext.Set<AppUser>()
                .Any(user => user.Id == firstUser.Id
                             && !user.IsDeleted
                             && user.UserStatus == AppUserStatus.Enable);

            exists.Should().BeTrue();
        }

        var response = await client.GetAsync(
            "/api/AppUser?pageIndex=1&pageSize=10&sortBy=createdAt&sortDirection=desc&keyword=page_user",
            TestContext.Current.CancellationToken);

        var responseText = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        Console.WriteLine(responseText);

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            responseText);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<PagedResultJson<AppUserDtoJson>>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Total.Should().Be(2);
        body.Data.Count.Should().Be(2);
        body.Data.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Update_ShouldReturnUpdatedUser()
    {
        await ResetAsync();

        var client = _factory.CreateClient();

        var createdUser = await CreateUserAsync(
            client,
            userName: "update_user",
            password: "Password123?",
            phoneNumber: "13900000104",
            email: "update_user@example.com");

        _factory.CurrentUser.Id = createdUser.Id;

        var response = await client.PutAsJsonAsync(
            $"/api/AppUser/{createdUser.Id}",
            new
            {
                UserName = "updated_user",
                Email = "updated_user@example.com",
                PhoneNumber = "13900000105",
                Sex = 0,
                UserStatus = 1
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppUserDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.UserName.Should().Be("updated_user");
        body.Data.Email.Should().Be("updated_user@example.com");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = dbContext.Set<AppUser>().Single(user => user.Id == createdUser.Id);

        user.UserName.Should().Be("updated_user");
        user.Email.Should().Be("updated_user@example.com");
        user.PhoneNumber.Should().Be("13900000105");
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnSuccess_WhenOldPasswordIsCorrect()
    {
        await ResetAsync();

        var client = _factory.CreateClient();

        var createdUser = await CreateUserAsync(
            client,
            userName: "change_password_user",
            password: "Password123?",
            phoneNumber: "13900000106",
            email: "change_password_user@example.com");

        _factory.CurrentUser.Id = createdUser.Id;

        var response = await client.PutAsJsonAsync(
            $"/api/AppUser/{createdUser.Id}/password",
            new
            {
                OldPassword = "Password123?",
                NewPassword = "NewPassword123?",
                ConfirmPassword = "NewPassword123?"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<object>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = dbContext.Set<AppUser>().Single(user => user.Id == createdUser.Id);

        user.PasswordHash.Should().Be("HASH::NewPassword123?");
    }

    [Fact]
    public async Task Delete_ShouldSoftDeleteUser()
    {
        await ResetAsync();

        var client = _factory.CreateClient();

        var createdUser = await CreateUserAsync(
            client,
            userName: "delete_api_user",
            password: "Password123?",
            phoneNumber: "13900000107",
            email: "delete_api_user@example.com");

        _factory.CurrentUser.Id = createdUser.Id;

        var response = await client.DeleteAsync(
            $"/api/AppUser/{createdUser.Id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<object>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = dbContext.Set<AppUser>().Single(user => user.Id == createdUser.Id);

        user.IsDeleted.Should().BeTrue();
        user.UserStatus.Should().Be(AppUserStatus.Void);
        user.DeletedAt.Should().NotBeNull();
    }

    private async Task ResetAsync()
    {
        _factory.CurrentUser.Id = null;
        await _factory.ResetDatabaseAsync();
    }

    //private static async Task<AppUserDtoJson> CreateUserAsync(
    //    HttpClient client,
    //    string userName,
    //    string password,
    //    string phoneNumber,
    //    string email)
    //{
    //    var response = await client.PostAsJsonAsync(
    //        "/api/AppUser",
    //        new
    //        {
    //            UserName = userName,
    //            Password = password,
    //            Sex = 2,
    //            PhoneNumber = phoneNumber,
    //            Email = email
    //        },
    //        TestContext.Current.CancellationToken);

    //    response.StatusCode.Should().Be(HttpStatusCode.OK);

    //    var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppUserDtoJson>>(
    //        cancellationToken: TestContext.Current.CancellationToken);

    //    body.Should().NotBeNull();
    //    body!.Success.Should().BeTrue();
    //    body.Data.Should().NotBeNull();

    //    return body.Data!;
    //}

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

        var responseText = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            responseText);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppUserDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue(responseText);
        body.Data.Should().NotBeNull(responseText);

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

    private sealed class PagedResultJson<T>
    {
        public long Total { get; set; }

        public int Count { get; set; }

        public List<T> Items { get; set; } = [];

        public PageMetadataJson Metadata { get; set; } = new();
    }

    private sealed class PageMetadataJson
    {
        public long Total { get; set; }

        public int Count { get; set; }

        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }

        public bool HasPrevious { get; set; }

        public bool HasNext { get; set; }
    }
}