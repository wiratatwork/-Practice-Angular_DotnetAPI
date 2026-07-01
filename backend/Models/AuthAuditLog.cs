namespace backend.Models
{
    public static class AuthAuditEventTypes
    {
        public const string LoginSuccess = "LoginSuccess";
        public const string LoginFailure = "LoginFailure";
        public const string RefreshSuccess = "RefreshSuccess";
        public const string RefreshFailure = "RefreshFailure";
        public const string LogoutSuccess = "LogoutSuccess";
        public const string LogoutFailure = "LogoutFailure";
    }

    public class AuthAuditLog
    {
        public long Id { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string EventType { get; set; } = string.Empty;
        public bool Succeeded { get; set; }
        public string? FailureReason { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime OccurredAtUtc { get; set; }
        public int? RefreshTokenId { get; set; }
        public string? MetadataJson { get; set; }

        public User? User { get; set; }
        public RefreshToken? RefreshToken { get; set; }
    }
}
