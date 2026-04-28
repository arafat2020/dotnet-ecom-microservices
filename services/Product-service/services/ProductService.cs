using product_service.Model;
using product_service.DTOs;
using Microsoft.EntityFrameworkCore;
using product_service.Interfaces;

namespace product_service.services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _context;
    public ProductService(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto)
    {
        var categoryExists = await _context.categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists) throw new Exception("Category not found");
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            BasePrice = dto.BasePrice,
            CategoryId = dto.CategoryId
        };

        _context.products.Add(product);
        await _context.SaveChangesAsync();
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            BasePrice = product.BasePrice,
            CategoryId = product.CategoryId
        };
    }

    // Implement product-related operations here, e.g. CreateProduct, GetProductById, etc.
}