using InprovePlan.ApiTests.Clients;
using InprovePlan.ApiTests.Infrastructure;
using InprovePlan.ApiTests.TestData;

namespace InprovePlan.ApiTests.Identity;

[Collection(WebApiTestCollection.Name)]
public sealed class IdentityControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public IdentityControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WhenUserNameAndPasswordAreCorrect_ShouldReturnAccessToken()
    {
        await _factory.ResetDatabaseAsync();

        var client = _factory.CreateClient();

        await AppUserApiClient.CreateUserAsync(
            client,
            AppUserTestData.ValidUserName,
            AppUserTestData.ValidPassword,
            AppUserTestData.ValidSex,
            AppUserTestData.ValidPhoneNumber,
            AppUserTestData.ValidEmail);

        var login = await IdentityApiClient.LoginAsync(
            client,
            AppUserTestData.ValidUserName,
            AppUserTestData.ValidPassword);

        login.AccessToken.Should().Be("test-access-token");
    }
}
