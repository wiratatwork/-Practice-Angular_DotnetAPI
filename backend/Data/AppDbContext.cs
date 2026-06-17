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
        }
    }
}
