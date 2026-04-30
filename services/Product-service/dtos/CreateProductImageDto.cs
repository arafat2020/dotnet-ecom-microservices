namespace product_service.DTOs;

public class CreateProductImageDto
{
    public Guid ImageFileId { get; set; }
    public string Url { get; set; } = null!;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
}
