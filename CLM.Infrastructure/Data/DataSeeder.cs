using BCrypt.Net;
using CLM.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CLM.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await context.Database.MigrateAsync();

        // Seed admin user if not exists
        if (!await context.Users.AnyAsync(u => u.Email == "admin@clm.com"))
        {
            var adminUser = new User
            {
                Name = "System Admin",
                Email = "admin@clm.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                RoleId = 1, // Admin
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(adminUser);
        }

        if (!await context.Users.AnyAsync(u => u.Email == "manager@clm.com"))
        {
            var manager = new User
            {
                Name = "Contract Manager",
                Email = "manager@clm.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
                RoleId = 2, // Manager
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(manager);
        }

        await context.SaveChangesAsync();
    }
}
