using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Data;

public static class SeedData
{
    public const string DefaultAdminUsername = "admin";
    public const string DefaultAdminPassword = "Admin@12345";

    public static async Task EnsureSeededAsync(IServiceProvider services, IConfiguration config)
    {
        var db = services.GetRequiredService<FleetDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        if (!await db.Users.AnyAsync())
        {
            var hasher = new PasswordHasher<User>();
            var admin = new User
            {
                Username = DefaultAdminUsername,
                Email = config["Seed:AdminEmail"] ?? "admin@example.com",
                FullName = "System Administrator",
                Role = UserRole.Admin,
                IsActive = true
            };
            admin.PasswordHash = hasher.HashPassword(admin, DefaultAdminPassword);
            db.Users.Add(admin);

            logger.LogWarning(
                "No users found. Seeded default admin account -- username: '{Username}', password: '{Password}'. Change this password immediately after first login.",
                DefaultAdminUsername, DefaultAdminPassword);
        }

        if (!await db.CompanyProfiles.AnyAsync())
        {
            db.CompanyProfiles.Add(new CompanyProfile
            {
                CompanyName = config["Seed:CompanyName"] ?? "My Transport Company"
            });
        }

        await db.SaveChangesAsync();
    }
}
