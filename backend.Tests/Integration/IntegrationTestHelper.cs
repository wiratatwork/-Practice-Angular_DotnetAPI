using System.Net.Http.Headers;
using System.Net.Http.Json;
using backend.Models;

namespace backend.Tests.Integration;

internal static class IntegrationTestHelper
{
    public static async Task<LoginResponse> LoginAsync(
        HttpClient client,
        string username,
        string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = username,
            Password = password,
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        return body;
    }

    public static async Task AuthenticateAsAsync(
        HttpClient client,
        string username,
        string password)
    {
        var body = await LoginAsync(client, username, password);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.AccessToken);
    }
}
