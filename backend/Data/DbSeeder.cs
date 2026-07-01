using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.Users.AnyAsync())
            {
                return;
            }

            var users = new[]
            {
                new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
                    Role = "Admin",
                },
                new User
                {
                    Username = "user",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@1234"),
                    Role = "User",
                },
            };

            context.Users.AddRange(users);
            await context.SaveChangesAsync();
        }
    }
}
