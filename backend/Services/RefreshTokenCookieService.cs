using Microsoft.AspNetCore.Http;

namespace backend.Services
{
    public class RefreshTokenCookieService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public RefreshTokenCookieService(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public string CookieName =>
            _configuration["Jwt:RefreshCookieName"] ?? "refresh_token";

        public string CookiePath =>
            _configuration["Jwt:RefreshCookiePath"] ?? "/api/auth";

        public void SetRefreshTokenCookie(HttpResponse response, string rawToken, DateTime expiresAtUtc)
        {
            var options = BuildCookieOptions(expiresAtUtc);
            response.Cookies.Append(CookieName, rawToken, options);
        }

        public void ClearRefreshTokenCookie(HttpResponse response)
        {
            var options = BuildCookieOptions(DateTime.UtcNow.AddDays(-1));
            response.Cookies.Delete(CookieName, options);
        }

        public string? GetRefreshTokenFromRequest(HttpRequest request)
        {
            return request.Cookies[CookieName];
        }

        private CookieOptions BuildCookieOptions(DateTime expiresAtUtc)
        {
            var secure = _configuration.GetValue<bool?>("Jwt:RefreshCookieSecure")
                ?? !_environment.IsDevelopment();

            return new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = SameSiteMode.Strict,
                Path = CookiePath,
                Expires = new DateTimeOffset(expiresAtUtc),
            };
        }
    }
}
