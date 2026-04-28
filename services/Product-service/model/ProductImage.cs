using System.ComponentModel.DataAnnotations;

namespace product_service.Model;

public class ProductImage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required]
    public string Url { get; set; } = null!; 
    // from MinIO / S3

    public string? AltText { get; set; }

    public bool IsPrimary { get; set; } = false;
}