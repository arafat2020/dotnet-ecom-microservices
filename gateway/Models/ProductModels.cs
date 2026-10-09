using System.ComponentModel.DataAnnotations;

namespace gateway.Models;

/// <summary>Request body for creating a product.</summary>
public class CreateProductRequestDto
{
    /// <summary>The product name.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Price used when a variant has no price of its own.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "BasePrice cannot be negative.")]
    public decimal BasePrice { get; set; }

    /// <summary>ID of an existing category.</summary>
    [Required]
    public string CategoryId { get; set; } = null!;

    /// <summary>Optional variants, e.g. "Red - XL".</summary>
    public List<ProductVariantDto> Variants { get; set; } = new();

    /// <summary>Images already uploaded through the image endpoints.</summary>
    public List<ProductImageDto> Images { get; set; } = new();
}

/// <summary>A variant of a product being created.</summary>
public class ProductVariantDto
{
    /// <summary>The variant name.</summary>
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>Overrides the base price when set.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative.")]
    public decimal? Price { get; set; }

    /// <summary>Units in stock.</summary>
    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
    public int Stock { get; set; }

    /// <summary>Optional stock keeping unit.</summary>
    public string? Sku { get; set; }
}

/// <summary>An uploaded image attached to a product being created.</summary>
public class ProductImageDto
{
    /// <summary>ID returned by the image upload endpoint.</summary>
    [Required]
    public string ImageFileId { get; set; } = null!;

    /// <summary>Public URL returned by the image upload endpoint.</summary>
    [Required]
    public string Url { get; set; } = null!;

    /// <summary>Optional alternative text.</summary>
    public string? AltText { get; set; }

    /// <summary>Whether this is the main image of the product.</summary>
    public bool IsPrimary { get; set; }
}
