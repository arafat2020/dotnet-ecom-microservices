using System.ComponentModel.DataAnnotations;

namespace product_service.Model;

public class Product
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [Required]
    public decimal BasePrice { get; set; }

    public bool IsActive { get; set; } = true;

    // 🔗 Category relation
    [Required]
    public Guid CategoryId { get; set; }
    public CategoryModel Category { get; set; } = null!;

    // 🧩 Navigation
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}