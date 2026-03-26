using Image_service.Model;
using Microsoft.EntityFrameworkCore;

namespace Image_service.Db;

public class ImageDbContext : DbContext
{
    public ImageDbContext(DbContextOptions<ImageDbContext> options) : base(options) { }

    public DbSet<FileMetadata> Files { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FileMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                  .HasDefaultValueSql("NEWID()");

            entity.Property(e => e.FileName).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Bucket).HasMaxLength(128).IsRequired();
            entity.Property(e => e.ObjectKey).HasMaxLength(1024).IsRequired();
            entity.Property(e => e.Url).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.ThumbnailObjectKey).HasMaxLength(1024);
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(2048);
            entity.Property(e => e.ContentType).HasMaxLength(256).IsRequired();
            entity.Property(e => e.EntityType)
                  .HasMaxLength(128)
                  .HasConversion<string>(); // Store enum as string
            entity.Property(e => e.EntityId).HasMaxLength(256);

            // Index for efficient lookup by owning entity
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });
    }
}
