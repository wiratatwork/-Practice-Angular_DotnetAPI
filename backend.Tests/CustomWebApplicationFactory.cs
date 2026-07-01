using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace backend.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=demo;Username=sa;Password=test";

        builder.UseEnvironment("Testing");

        // UseSetting overrides appsettings.json (which has a different local password).
        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        builder.UseSetting("Jwt:Key", "BasicApp-SuperSecret-JWT-Key-256bit-Minimum-Length-Required!");
        builder.UseSetting("Jwt:Issuer", "BasicApp");
        builder.UseSetting("Jwt:Audience", "BasicApp");
        builder.UseSetting("Jwt:AccessTokenExpiryMinutes", "15");
        builder.UseSetting("Jwt:RefreshTokenSlidingDays", "7");
        builder.UseSetting("Jwt:RefreshCookieName", "refresh_token");
        builder.UseSetting("Jwt:RefreshCookiePath", "/api/auth");
        builder.UseSetting("Jwt:RefreshCookieSecure", "false");
    }
}
