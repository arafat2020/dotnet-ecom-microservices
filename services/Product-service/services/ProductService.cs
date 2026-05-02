using product_service.Model;
using product_service.DTOs;
using Microsoft.EntityFrameworkCore;
using product_service.Interfaces;
using Shared.Protos.Image;
using Elastic.Clients.Elasticsearch;
namespace product_service.services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _context;
    private readonly ILogger<ProductService> _logger;
    private readonly ImageService.ImageServiceClient _imageServiceClient;
    private readonly ElasticsearchClient _elasticsearchClient;

    public ProductService(
        ProductDbContext context, 
        ILogger<ProductService> logger,
        ImageService.ImageServiceClient imageServiceClient,
        ElasticsearchClient elasticsearchClient)
    {
        _context = context;
        _logger = logger;
        _imageServiceClient = imageServiceClient;
        _elasticsearchClient = elasticsearchClient;
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

            if (dto.Images != null && dto.Images.Any())
            {
                var images = dto.Images.Select(i => new ProductImage
                {
                    ProductId = product.Id,
                    ImageFileId = i.ImageFileId,
                    Url = i.Url,
                    AltText = i.AltText,
                    IsPrimary = i.IsPrimary
                }).ToList();

                _context.productImages.AddRange(images);
                await _context.SaveChangesAsync();
            }

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

            var responseDto = new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                BasePrice = product.BasePrice,
                CategoryId = product.CategoryId
            };

            // Index to Elasticsearch
            try
            {
                var response = await _elasticsearchClient.IndexAsync(responseDto, i => i.Index("products").Id(responseDto.Id));
                if (!response.IsValidResponse)
                {
                    _logger.LogWarning("Failed to index product {ProductId} to Elasticsearch: {Error}", responseDto.Id, response.DebugInformation);
                }
                else
                {
                    _logger.LogInformation("Successfully indexed product {ProductId} to Elasticsearch.", responseDto.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while indexing product {ProductId} to Elasticsearch.", responseDto.Id);
            }

            return responseDto;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating product. Transaction rolled back.");

            // Rollback: delete images from Image-service
            if (dto.Images != null && dto.Images.Any())
            {
                try
                {
                    var imageIds = dto.Images.Select(i => i.ImageFileId.ToString()).ToList();
                    var bulkDeleteRequest = new BulkDeleteImageRequest();
                    bulkDeleteRequest.ImageIds.AddRange(imageIds);

                    await _imageServiceClient.BulkDeleteImageAsync(bulkDeleteRequest);
                    _logger.LogInformation("Successfully requested bulk deletion of {Count} images during product creation rollback.", imageIds.Count);
                }
                catch (Exception grpcEx)
                {
                    _logger.LogError(grpcEx, "Failed to call Image-service to rollback images for product creation.");
                }
            }

            throw;
        }

    }

    // Implement product-related operations here, e.g. CreateProduct, GetProductById, etc.
}