namespace backend.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? LastUsedAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
        public string? RevokedReason { get; set; }
        public int? ReplacedByTokenId { get; set; }
        public string? CreatedByIp { get; set; }
        public string? CreatedByUserAgent { get; set; }
        public string? LastUsedByIp { get; set; }
        public string? LastUsedByUserAgent { get; set; }

        public User User { get; set; } = null!;
        public RefreshToken? ReplacedByToken { get; set; }
    }
}
