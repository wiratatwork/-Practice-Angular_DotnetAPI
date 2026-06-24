using System.Net.Http.Json;
using backend.Models;
using backend.Tests;

namespace backend.Tests.Integration;

[Collection("Integration")]
public class AuthIntegrationTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsAccessToken()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = "admin",
            Password = "Admin@1234",
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.Equal("admin", body.User.Username);
    }
}
