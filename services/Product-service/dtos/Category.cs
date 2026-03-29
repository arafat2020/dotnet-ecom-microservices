namespace product_service.DTOs;

public class CreateCategoryDto
{
    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
}

public class CategoryResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
}