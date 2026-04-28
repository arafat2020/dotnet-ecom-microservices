using product_service.Model;
using product_service.DTOs;
using Microsoft.EntityFrameworkCore;
using product_service.Interfaces;

namespace product_service.services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _context;
    private readonly Logger<ProductService> _logger;
    public ProductService(ProductDbContext context, Logger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto)
    {
        var categoryExists = await _context.categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists) throw new Exception("Category not found");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                BasePrice = dto.BasePrice,
                CategoryId = dto.CategoryId
            };

            _context.products.Add(product);
            await _context.SaveChangesAsync();

            if (dto.Variants.Any())
            {
                var variants = dto.Variants.Select(v => new ProductVariant
                {
                    ProductId = product.Id,
                    Name = v.Name,
                    Price = v.Price,
                    Stock = v.Stock,
                    SKU = v.SKU
                }).ToList();

                _context.productVariants.AddRange(variants);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                BasePrice = product.BasePrice,
                CategoryId = product.CategoryId
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating product. Transaction rolled back.");
            throw;
        }

    }

    // Implement product-related operations here, e.g. CreateProduct, GetProductById, etc.
}