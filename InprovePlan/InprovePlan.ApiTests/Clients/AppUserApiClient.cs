using InprovePlan.ApiTests.Contracts;
using InprovePlan.Domain.Entities;

namespace InprovePlan.ApiTests.Clients;

public static class AppUserApiClient
{
    public static async Task<AppUserDtoJson> CreateUserAsync(
        HttpClient client,
        string userName,
        string password,
        AppUserSex sex,
        string phoneNumber,
        string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/AppUser",
            new
            {
                UserName = userName,
                Password = password,
                Sex = sex,
                PhoneNumber = phoneNumber,
                Email = email
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppUserDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }

    public static async Task<AppUserDtoJson> UpdateUserAsync(
        HttpClient client,
        long id,
        string userName,
        string email,
        string? phoneNumber,
        AppUserSex sex,
        AppUserStatus userStatus)
    {
        var response = await client.PutAsJsonAsync(
            $"/api/AppUser/{id}",
            new
            {
                UserName = userName,
                Email = email,
                PhoneNumber = phoneNumber,
                Sex = sex,
                UserStatus = userStatus
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppUserDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }

    public static async Task ChangePasswordAsync(
        HttpClient client,
        long id,
        string oldPassword,
        string newPassword,
        string confirmPassword)
    {
        var response = await client.PutAsJsonAsync(
            $"/api/AppUser/{id}/password",
            new
            {
                OldPassword = oldPassword,
                NewPassword = newPassword,
                ConfirmPassword = confirmPassword
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<object>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
    }

    public static async Task DeleteUserAsync(HttpClient client, long id)
    {
        var response = await client.DeleteAsync(
            $"/api/AppUser/{id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<object>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
    }

    public static async Task<AppUserDtoJson> GetUserByIdAsync(HttpClient client, long id)
    {
        var response = await client.GetAsync(
            $"/api/AppUser/{id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<AppUserDtoJson>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }

    public static async Task<PagedResultJson<AppUserDtoJson>> GetUsersPagedAsync(
        HttpClient client,
        string? keyword = null,
        AppUserStatus? status = null,
        AppUserSex? sex = null,
        bool includeDeleted = false)
    {
        var query = $"/api/AppUser?pageIndex=1&pageSize=10&sortBy=createdAt&sortDirection=desc&includeDeleted={includeDeleted.ToString().ToLowerInvariant()}";

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query += $"&keyword={Uri.EscapeDataString(keyword)}";
        }

        if (status.HasValue)
        {
            query += $"&status={(int)status.Value}";
        }

        if (sex.HasValue)
        {
            query += $"&sex={(int)sex.Value}";
        }

        var response = await client.GetAsync(query, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseJson<PagedResultJson<AppUserDtoJson>>>(
            cancellationToken: TestContext.Current.CancellationToken);

        body!.Success.Should().BeTrue();
        return body.Data!;
    }
}
