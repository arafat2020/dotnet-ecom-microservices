using Microsoft.EntityFrameworkCore;
using auth_service.Model;

namespace db.AuthDbContext;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options){}

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(100);
            e.HasIndex(u => u.Username)
            .IsUnique();

            e.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);
            e.HasIndex(u => u.Email)
            .IsUnique();

        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(r => r.Name)
                .IsUnique(); // prevent duplicate roles
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            // Composite Key
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            // Relationship: User → UserRoles
            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: Role → UserRoles
            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
    
}