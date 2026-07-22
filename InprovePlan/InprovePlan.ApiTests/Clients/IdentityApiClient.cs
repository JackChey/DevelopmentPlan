using InprovePlan.ApiTests.Contracts;

namespace InprovePlan.ApiTests.Clients;

public static class IdentityApiClient
{
    public static async Task<LoginAppUserDtoJson> LoginAsync(
        HttpClient client,
        string userName,
        string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/Identity",
            new
            {
                UserName = userName,
                Password = password
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<LoginAppUserDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }
}
