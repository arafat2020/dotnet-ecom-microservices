namespace product_service.DTOs;

public class ProductDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsActive { get; set; }

    public List<ProductVariantDetailDto> Variants { get; set; } = new();
    public List<CreateProductImageDto> Images { get; set; } = new();
}

public class ProductVariantDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? SKU { get; set; }
}
