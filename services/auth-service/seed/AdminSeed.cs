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
        var roleNames = new[] { "SuperAdmin", "Admin", "User" };
        foreach (var roleName in roleNames)
        {
            if (!await dbContext.Roles.AnyAsync(r => r.Name == roleName))
            {
                dbContext.Roles.Add(new Role { Name = roleName });
            }
        }
        await dbContext.SaveChangesAsync();

        // ── 2. Read admin credentials from config / env ──────────────────────
        // Emails are stored lower-cased, the same way registration stores them
        var adminEmail    = (configuration["AdminSeed:Email"]    ?? "admin@ecom.local").Trim().ToLowerInvariant();
        var adminUsername = configuration["AdminSeed:Username"] ?? "admin";
        var adminPassword = configuration["AdminSeed:Password"] ?? "Admin@12345";

        // ── 3. Read super admin credentials from config / env ────────────────
        var superAdminEmail    = (configuration["SuperAdminSeed:Email"]    ?? "superadmin@ecom.local").Trim().ToLowerInvariant();
        var superAdminUsername = configuration["SuperAdminSeed:Username"] ?? "superadmin";
        var superAdminPassword = configuration["SuperAdminSeed:Password"] ?? "SuperAdmin@12345";

        // ── 4. Skip/create the admin user ────────────────────────────────────
        if (!await dbContext.Users.AnyAsync(u => u.Email == adminEmail))
        {
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
            Console.WriteLine($"[AdminSeed] Admin user '{adminUsername}' created successfully.");
        }

        // ── 5. Skip/create the superadmin user ────────────────────────────────
        if (!await dbContext.Users.AnyAsync(u => u.Email == superAdminEmail))
        {
            var superAdminRole = await dbContext.Roles.FirstAsync(r => r.Name == "SuperAdmin");

            var superAdminUser = new User
            {
                Username     = superAdminUsername,
                Email        = superAdminEmail,
                PasswordHash = AuthUtils.HashPassword(superAdminPassword),
            };

            superAdminUser.UserRoles.Add(new UserRole
            {
                User   = superAdminUser,
                RoleId = superAdminRole.Id,
            });

            dbContext.Users.Add(superAdminUser);
            Console.WriteLine($"[AdminSeed] SuperAdmin user '{superAdminUsername}' created successfully.");
        }

        await dbContext.SaveChangesAsync();
    }
}

