using Grpc.Core;
using product_service.Interfaces;
using product_service.DTOs;
using Shared.Protos.Product;

namespace Product_service.GrpcServices;

/// <summary>
/// gRPC Service for Product/Category operations.
/// This service allows the API Gateway to create categories securely.
/// </summary>
public class ProductGrpcService : Shared.Protos.Product.ProductService.ProductServiceBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<ProductGrpcService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductGrpcService"/> class.
    /// </summary>
    /// <param name="categoryService">The core service for category operations.</param>
    /// <param name="logger">The logger instance.</param>
    public ProductGrpcService(ICategoryService categoryService, ILogger<ProductGrpcService> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="request">The creation request containing name and description.</param>
    /// <param name="context">The gRPC call context.</param>
    /// <returns>A response containing the created category details.</returns>
    public override async Task<CreateCategoryResponse> CreateCategory(CreateCategoryRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreateCategoryDto
            {
                Name = request.Name,
                Description = request.Description
            };

            var result = await _categoryService.CreateAsync(dto);

            return new CreateCategoryResponse
            {
                Id = result.Id.ToString(),
                Name = result.Name,
                Description = result.Description,
                Success = true,
                Message = "Category created successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category via gRPC.");
            return new CreateCategoryResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets all categories.
    /// </summary>
    /// <param name="request">The get categories request.</param>
    /// <param name="context">The gRPC call context.</param>
    /// <returns>A response containing the list of categories.</returns>
    public override async Task<GetCategoriesResponse> GetCategories(GetCategoriesRequest request, ServerCallContext context)
    {
        try
        {
            var categories = await _categoryService.GetAllAsync();
            var response = new GetCategoriesResponse
            {
                Success = true,
                Message = "Categories retrieved successfully."
            };

            foreach (var category in categories)
            {
                response.Categories.Add(new CategoryDto
                {
                    Id = category.Id.ToString(),
                    Name = category.Name ?? "",
                    Description = category.Description ?? ""
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories via gRPC.");
            return new GetCategoriesResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}
