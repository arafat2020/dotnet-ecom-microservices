using auth_service.Model;
using auth_service.Utils;
using db.AuthDbContext;
using Microsoft.EntityFrameworkCore;

namespace auth_service.Seed;

public static class AdminSeed
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        // Apply any pending migrations
        await dbContext.Database.MigrateAsync();

        // ── 1. Ensure roles exist ────────────────────────────────────────────
        var roleNames = new[] { "Admin", "User" };
        foreach (var roleName in roleNames)
        {
            if (!await dbContext.Roles.AnyAsync(r => r.Name == roleName))
            {
                dbContext.Roles.Add(new Role { Name = roleName });
            }
        }
        await dbContext.SaveChangesAsync();

        // ── 2. Read admin credentials from config / env ──────────────────────
        var adminEmail    = configuration["AdminSeed:Email"]    ?? "admin@ecom.local";
        var adminUsername = configuration["AdminSeed:Username"] ?? "admin";
        var adminPassword = configuration["AdminSeed:Password"] ?? "Admin@12345";

        // ── 3. Skip if the admin already exists ──────────────────────────────
        if (await dbContext.Users.AnyAsync(u => u.Email == adminEmail))
        {
            Console.WriteLine("[AdminSeed] Admin user already exists – skipping.");
            return;
        }

        // ── 4. Create the admin user ─────────────────────────────────────────
        var adminRole = await dbContext.Roles.FirstAsync(r => r.Name == "Admin");

        var adminUser = new User
        {
            Username     = adminUsername,
            Email        = adminEmail,
            PasswordHash = AuthUtils.HashPassword(adminPassword),
        };

        adminUser.UserRoles.Add(new UserRole
        {
            User   = adminUser,
            RoleId = adminRole.Id,
        });

        dbContext.Users.Add(adminUser);
        await dbContext.SaveChangesAsync();

        Console.WriteLine($"[AdminSeed] Admin user '{adminUsername}' created successfully.");
    }
}
