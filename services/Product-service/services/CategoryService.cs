using Microsoft.EntityFrameworkCore;
using product_service.Interfaces;
using product_service.Model;
using product_service.DTOs;

namespace product_service.services;

public class CategoryService : ICategoryService
{
    private readonly ProductDbContext _context;
    public CategoryService(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto)
    {
        var category = new CategoryModel()
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            ParentId = dto.ParentId
        };

        _context.categories.Add(category);
        await _context.SaveChangesAsync();
        return new CategoryResponseDto()
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ParentId = category.ParentId
        };
    }

    public async Task<List<CategoryResponseDto>> GetAllAsync()
    {
        var categories = await _context.categories.Select(c => new CategoryResponseDto()
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ParentId = c.ParentId
        }).ToListAsync();
        return categories;
    }
}