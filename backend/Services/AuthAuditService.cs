using System.Text.Json;
using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class AuthAuditService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthAuditService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            string eventType,
            bool succeeded,
            string? username = null,
            int? userId = null,
            string? failureReason = null,
            int? refreshTokenId = null,
            object? metadata = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var auditLog = new AuthAuditLog
            {
                EventType = eventType,
                Succeeded = succeeded,
                Username = username,
                UserId = userId,
                FailureReason = failureReason,
                RefreshTokenId = refreshTokenId,
                IpAddress = GetClientIp(httpContext),
                UserAgent = httpContext?.Request.Headers.UserAgent.ToString(),
                CorrelationId = httpContext?.TraceIdentifier,
                OccurredAtUtc = DateTime.UtcNow,
                MetadataJson = metadata == null ? null : JsonSerializer.Serialize(metadata),
            };

            _context.AuthAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        private static string? GetClientIp(HttpContext? httpContext)
        {
            if (httpContext == null)
            {
                return null;
            }

            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            return httpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
}
