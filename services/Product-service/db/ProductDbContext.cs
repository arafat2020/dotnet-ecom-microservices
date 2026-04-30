using Microsoft.EntityFrameworkCore;
using product_service.Model;

public class ProductDbContext: DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options): base(options){}

    public DbSet<CategoryModel> categories {get; set;} = null!;
    public DbSet<Product> products {get; set;} = null!;
    public DbSet<ProductVariant> productVariants {get; set;} = null!;
    public DbSet<ProductImage> productImages {get; set;} = null!;

    public DbSet<ProductVariant> productVariantOptions {get; set;} = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CategoryModel>()
        .HasOne(c=>c.Parent)
        .WithMany(c=>c.Children)
        .HasForeignKey(c=>c.ParentId)
        .OnDelete(DeleteBehavior.Restrict);
    }
}