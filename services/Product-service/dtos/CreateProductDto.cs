namespace product_service.DTOs;

public class CreateProductDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public Guid CategoryId { get; set; }

    public List<CreateProductVariantDto> Variants { get; set; } = new();
    public List<CreateProductImageDto>? Images { get; set; } = new();
}