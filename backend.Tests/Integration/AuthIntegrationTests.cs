using System.Net;
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

        var body = await IntegrationTestHelper.LoginAsync(client, "admin", "Admin@1234");

        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.Equal("admin", body.User.Username);
        Assert.Equal("Admin", body.User.Role);
    }

    [Fact]
    public async Task Login_WithSeededUser_ReturnsAccessToken()
    {
        using var client = factory.CreateClient();

        var body = await IntegrationTestHelper.LoginAsync(client, "user", "User@1234");

        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.Equal("user", body.User.Username);
        Assert.Equal("User", body.User.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = "admin",
            Password = "WrongPassword",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithEmptyUsername_ReturnsBadRequest()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = "",
            Password = "Admin@1234",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_AfterLogin_ReturnsNewAccessToken()
    {
        using var client = factory.CreateClient();

        var loginBody = await IntegrationTestHelper.LoginAsync(client, "admin", "Admin@1234");

        var response = await client.PostAsync("/api/auth/refresh", null);

        response.EnsureSuccessStatusCode();

        var refreshBody = await response.Content.ReadFromJsonAsync<RefreshResponse>();
        Assert.NotNull(refreshBody);
        Assert.False(string.IsNullOrWhiteSpace(refreshBody.AccessToken));
        Assert.NotEqual(loginBody.AccessToken, refreshBody.AccessToken);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_AfterLogin_ReturnsNoContent()
    {
        using var client = factory.CreateClient();

        await IntegrationTestHelper.LoginAsync(client, "admin", "Admin@1234");

        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutCookie_ReturnsNoContent()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
