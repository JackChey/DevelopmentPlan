using InprovePlan.ApiTests.Clients;
using InprovePlan.ApiTests.Infrastructure;
using InprovePlan.ApiTests.TestData;
using InprovePlan.Domain.Entities;

namespace InprovePlan.ApiTests.AppUsers;

[Collection(WebApiTestCollection.Name)]
public sealed class AppUserControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AppUserControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WhenRequestValid_ShouldCreateUser()
    {
        var client = await CreateCleanClientAsync();

        var user = await AppUserApiClient.CreateUserAsync(
            client,
            AppUserTestData.ValidUserName,
            AppUserTestData.ValidPassword,
            AppUserTestData.ValidSex,
            AppUserTestData.ValidPhoneNumber,
            AppUserTestData.ValidEmail);

        user.Id.Should().BeGreaterThan(0);
        user.UserName.Should().Be(AppUserTestData.ValidUserName);
    }

    [Fact]
    public async Task Update_WhenUserExists_ShouldUpdateUser()
    {
        var (client, user) = await CreateClientWithCurrentUserAsync();

        var updated = await AppUserApiClient.UpdateUserAsync(
            client,
            user.Id,
            "UpdatedUser",
            "updated@example.com",
            "13900000001",
            AppUserSex.Male,
            AppUserStatus.Frozen);

        updated.Id.Should().Be(user.Id);
        updated.UserName.Should().Be("UpdatedUser");
        updated.Email.Should().Be("updated@example.com");
        updated.UserStatus.Should().Be((int)AppUserStatus.Frozen);
    }

    [Fact]
    public async Task ChangePassword_WhenOldPasswordIsCorrect_ShouldSucceed()
    {
        var (client, user) = await CreateClientWithCurrentUserAsync();

        await AppUserApiClient.ChangePasswordAsync(
            client,
            user.Id,
            AppUserTestData.ValidPassword,
            "NewPassword123!",
            "NewPassword123!");
    }

    [Fact]
    public async Task Delete_WhenUserExists_ShouldDeleteUser()
    {
        var (client, user) = await CreateClientWithCurrentUserAsync();

        await AppUserApiClient.DeleteUserAsync(client, user.Id);
    }

    [Fact]
    public async Task GetById_WhenUserExists_ShouldReturnUser()
    {
        var (client, user) = await CreateClientWithCurrentUserAsync();

        var found = await AppUserApiClient.GetUserByIdAsync(client, user.Id);

        found.Id.Should().Be(user.Id);
        found.UserName.Should().Be(AppUserTestData.ValidUserName);
    }

    [Fact]
    public async Task GetPaged_WhenUsersExist_ShouldReturnPagedUsers()
    {
        var (client, user) = await CreateClientWithCurrentUserAsync();

        var page = await AppUserApiClient.GetUsersPagedAsync(
            client,
            keyword: AppUserTestData.ValidUserName,
            status: AppUserStatus.Enable,
            sex: AppUserSex.Secret);

        page.Total.Should().Be(1);
        page.Items.Should().ContainSingle(item => item.Id == user.Id);
    }

    private async Task<HttpClient> CreateCleanClientAsync()
    {
        await _factory.ResetDatabaseAsync();
        return _factory.CreateClient();
    }

    private async Task<(HttpClient Client, Contracts.AppUserDtoJson User)> CreateClientWithCurrentUserAsync()
    {
        var client = await CreateCleanClientAsync();

        var user = await AppUserApiClient.CreateUserAsync(
            client,
            AppUserTestData.ValidUserName,
            AppUserTestData.ValidPassword,
            AppUserTestData.ValidSex,
            AppUserTestData.ValidPhoneNumber,
            AppUserTestData.ValidEmail);

        _factory.CurrentUser.Id = user.Id;

        return (client, user);
    }
}
