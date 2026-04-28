using product_service.DTOs;

namespace product_service.Interfaces;

public interface ICategoryService
{
    Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto);
    Task<List<CategoryResponseDto>> GetAllAsync();
}

