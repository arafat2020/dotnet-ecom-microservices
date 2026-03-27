using Microsoft.EntityFrameworkCore;
using product_service.Model;

public class ProductDbContext: DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options): base(options){}

    public DbSet<CategoryModel> categories {get; set;} = null!;

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