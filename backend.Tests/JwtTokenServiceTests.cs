using backend.Models;
using backend.Services;
using Microsoft.Extensions.Configuration;

namespace backend.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyToken()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "BasicApp-SuperSecret-JWT-Key-256bit-Minimum-Length-Required!",
                ["Jwt:Issuer"] = "BasicApp",
                ["Jwt:Audience"] = "BasicApp",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
            })
            .Build();

        var service = new JwtTokenService(configuration);
        var user = new User
        {
            Id = 1,
            Username = "admin",
            Role = "Admin",
            PasswordHash = "hash",
        };

        var (accessToken, expiresIn) = service.GenerateAccessToken(user);

        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.True(expiresIn > 0);
    }
}
