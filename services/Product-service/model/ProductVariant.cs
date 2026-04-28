using System.ComponentModel.DataAnnotations;

namespace product_service.Model;

public class ProductVariant
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!; 
    // e.g. "Red - XL"

    public decimal? Price { get; set; } 
    // override base price

    public int Stock { get; set; }

    public string? SKU { get; set; }
}