using System.Security.Cryptography;
using System.Text;
using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class RefreshTokenOperationResult
    {
        public bool Success { get; set; }
        public User? User { get; set; }
        public string? RawToken { get; set; }
        public int? RefreshTokenId { get; set; }
        public string? FailureReason { get; set; }
        public bool ReuseDetected { get; set; }
    }

    public class RefreshTokenService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public RefreshTokenService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<RefreshTokenOperationResult> CreateAsync(
            User user,
            string? ipAddress,
            string? userAgent)
        {
            var rawToken = GenerateRawToken();
            var slidingDays = GetSlidingDays();
            var now = DateTime.UtcNow;

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = HashToken(rawToken),
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddDays(slidingDays),
                CreatedByIp = ipAddress,
                CreatedByUserAgent = userAgent,
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return new RefreshTokenOperationResult
            {
                Success = true,
                User = user,
                RawToken = rawToken,
                RefreshTokenId = refreshToken.Id,
            };
        }

        public async Task<RefreshTokenOperationResult> RotateAsync(
            string rawToken,
            string? ipAddress,
            string? userAgent)
        {
            var tokenHash = HashToken(rawToken);
            var now = DateTime.UtcNow;

            var existingToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (existingToken == null)
            {
                return new RefreshTokenOperationResult
                {
                    Success = false,
                    FailureReason = "Invalid refresh token",
                };
            }

            if (existingToken.RevokedAtUtc != null)
            {
                await RevokeUserTokenFamilyAsync(existingToken.UserId, "Refresh token reuse detected");

                return new RefreshTokenOperationResult
                {
                    Success = false,
                    FailureReason = "Refresh token has been revoked",
                    ReuseDetected = true,
                    User = existingToken.User,
                    RefreshTokenId = existingToken.Id,
                };
            }

            if (existingToken.ExpiresAtUtc <= now)
            {
                existingToken.RevokedAtUtc = now;
                existingToken.RevokedReason = "Refresh token expired";
                await _context.SaveChangesAsync();

                return new RefreshTokenOperationResult
                {
                    Success = false,
                    FailureReason = "Refresh token expired",
                    RefreshTokenId = existingToken.Id,
                    User = existingToken.User,
                };
            }

            var slidingDays = GetSlidingDays();
            var newRawToken = GenerateRawToken();
            var replacement = new RefreshToken
            {
                UserId = existingToken.UserId,
                TokenHash = HashToken(newRawToken),
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddDays(slidingDays),
                CreatedByIp = existingToken.CreatedByIp,
                CreatedByUserAgent = existingToken.CreatedByUserAgent,
                LastUsedAtUtc = now,
                LastUsedByIp = ipAddress,
                LastUsedByUserAgent = userAgent,
            };

            _context.RefreshTokens.Add(replacement);
            await _context.SaveChangesAsync();

            existingToken.LastUsedAtUtc = now;
            existingToken.LastUsedByIp = ipAddress;
            existingToken.LastUsedByUserAgent = userAgent;
            existingToken.RevokedAtUtc = now;
            existingToken.RevokedReason = "Rotated";
            existingToken.ReplacedByTokenId = replacement.Id;

            await _context.SaveChangesAsync();

            return new RefreshTokenOperationResult
            {
                Success = true,
                User = existingToken.User,
                RawToken = newRawToken,
                RefreshTokenId = replacement.Id,
            };
        }

        public async Task<RefreshTokenOperationResult> RevokeAsync(string rawToken, string reason)
        {
            var tokenHash = HashToken(rawToken);
            var existingToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (existingToken == null)
            {
                return new RefreshTokenOperationResult
                {
                    Success = false,
                    FailureReason = "Refresh token not found",
                };
            }

            if (existingToken.RevokedAtUtc == null)
            {
                existingToken.RevokedAtUtc = DateTime.UtcNow;
                existingToken.RevokedReason = reason;
                await _context.SaveChangesAsync();
            }

            return new RefreshTokenOperationResult
            {
                Success = true,
                User = existingToken.User,
                RefreshTokenId = existingToken.Id,
            };
        }

        private async Task RevokeUserTokenFamilyAsync(int userId, string reason)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var token in activeTokens)
            {
                token.RevokedAtUtc = now;
                token.RevokedReason = reason;
            }

            await _context.SaveChangesAsync();
        }

        private int GetSlidingDays()
        {
            return int.TryParse(_configuration["Jwt:RefreshTokenSlidingDays"], out var days)
                ? days
                : 7;
        }

        public static string GenerateRawToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static string HashToken(string rawToken)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToBase64String(hash);
        }
    }
}
