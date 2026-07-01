using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Machine> Machines { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<AuthAuditLog> AuthAuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Machine>(entity =>
            {
                entity.HasKey(e => e.MachineNo);

                entity.Property(e => e.MachineNo)
                    .HasColumnName("machineno")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.MachineName)
                    .HasColumnName("machinename")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Plant)
                    .HasColumnName("plant")
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasMaxLength(10)
                    .IsRequired();

                entity.ToTable("machine");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.Username)
                    .HasColumnName("username")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(e => e.Username)
                    .IsUnique();

                entity.Property(e => e.PasswordHash)
                    .HasColumnName("passwordhash")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.Role)
                    .HasColumnName("role")
                    .HasMaxLength(20)
                    .IsRequired();

                entity.ToTable("users");
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
                entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
                entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
                entity.Property(e => e.LastUsedAtUtc).HasColumnName("last_used_at_utc");
                entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
                entity.Property(e => e.RevokedReason).HasColumnName("revoked_reason").HasMaxLength(100);
                entity.Property(e => e.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
                entity.Property(e => e.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(64);
                entity.Property(e => e.CreatedByUserAgent).HasColumnName("created_by_user_agent").HasMaxLength(512);
                entity.Property(e => e.LastUsedByIp).HasColumnName("last_used_by_ip").HasMaxLength(64);
                entity.Property(e => e.LastUsedByUserAgent).HasColumnName("last_used_by_user_agent").HasMaxLength(512);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAtUtc);
                entity.HasIndex(e => e.RevokedAtUtc);
                entity.HasIndex(e => e.TokenHash).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ReplacedByToken)
                    .WithMany()
                    .HasForeignKey(e => e.ReplacedByTokenId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.ToTable("refresh_tokens");
            });

            modelBuilder.Entity<AuthAuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50);
                entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Succeeded).HasColumnName("succeeded");
                entity.Property(e => e.FailureReason).HasColumnName("failure_reason").HasMaxLength(255);
                entity.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
                entity.Property(e => e.UserAgent).HasColumnName("user_agent").HasMaxLength(512);
                entity.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
                entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
                entity.Property(e => e.RefreshTokenId).HasColumnName("refresh_token_id");
                entity.Property(e => e.MetadataJson).HasColumnName("metadata_json");

                entity.HasIndex(e => e.OccurredAtUtc);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => new { e.Succeeded, e.OccurredAtUtc });

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.RefreshToken)
                    .WithMany()
                    .HasForeignKey(e => e.RefreshTokenId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.ToTable("auth_audit_logs");
            });
        }
    }
}
