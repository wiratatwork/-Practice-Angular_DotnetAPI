using backend.Models;
using backend.Services;
using Microsoft.Extensions.Configuration;

namespace backend.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyToken() // ทดสอบ GenerateAccessToken โดยไม่ต่อ Application
    {
        var configuration = new ConfigurationBuilder() // สร้าง ConfigurationBuilder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "BasicApp-SuperSecret-JWT-Key-256bit-Minimum-Length-Required!",
                ["Jwt:Issuer"] = "BasicApp",
                ["Jwt:Audience"] = "BasicApp",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
            })
            .Build();

        var service = new JwtTokenService(configuration); // สร้าง JwtTokenService
        var user = new User // สร้าง User
        {
            Id = 1,
            Username = "admin",
            Role = "Admin",
            PasswordHash = "hash",
        };

        var (accessToken, expiresIn) = service.GenerateAccessToken(user); // ทดสอบ GenerateAccessToken

        Assert.False(string.IsNullOrWhiteSpace(accessToken)); // ตรวจสอบว่า accessToken ไม่เป็นค่าว่าง
        Assert.True(expiresIn > 0); // ตรวจสอบว่า expiresIn มากกว่า 0
    }
}
