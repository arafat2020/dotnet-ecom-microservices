public class CreateProductVariantDto
{
    public string Name { get; set; } = null!;
    public decimal? Price { get; set; }
    public int Stock { get; set; }
    public string? SKU { get; set; }
}