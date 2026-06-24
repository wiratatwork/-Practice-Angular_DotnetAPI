using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace backend.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var connectionString = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
                ?? "Host=localhost;Port=5432;Database=demo;Username=sa;Password=test";

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Jwt:Key"] = "BasicApp-SuperSecret-JWT-Key-256bit-Minimum-Length-Required!",
                ["Jwt:Issuer"] = "BasicApp",
                ["Jwt:Audience"] = "BasicApp",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenSlidingDays"] = "7",
                ["Jwt:RefreshCookieName"] = "refresh_token",
                ["Jwt:RefreshCookiePath"] = "/api/auth",
                ["Jwt:RefreshCookieSecure"] = "false",
                ["Cors:AllowedOrigins:0"] = "http://localhost:4200",
            });
        });
    }
}
