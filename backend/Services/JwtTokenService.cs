using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Models;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services
{
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (string AccessToken, int ExpiresInSeconds) GenerateAccessToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key is not configured.");
            var issuer = _configuration["Jwt:Issuer"] ?? "BasicApp";
            var audience = _configuration["Jwt:Audience"] ?? "BasicApp";
            var expiryMinutes = int.TryParse(_configuration["Jwt:AccessTokenExpiryMinutes"], out var minutes)
                ? minutes
                : 15;

            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            var expiresIn = (int)Math.Max(1, (expiresAt - DateTime.UtcNow).TotalSeconds);

            return (accessToken, expiresIn);
        }
    }
}
