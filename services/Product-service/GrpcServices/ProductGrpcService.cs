using Shared.Protos.Product;
using product_service.DTOs;
using product_service.Interfaces;
using Grpc.Core;

namespace Product_service.GrpcServices;

/// <summary>
/// gRPC Service for Product and Category operations.
/// One class implements the whole <c>product.ProductService</c> contract: mapping two
/// implementations of the same service makes every call fail with an ambiguous route.
/// </summary>
public class ProductGrpcService: ProductService.ProductServiceBase
{
    private readonly IProductService _productService;
    private readonly IProductSearchService _productSearchService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<ProductGrpcService> _logger;

    public ProductGrpcService(
        IProductService productService,
        IProductSearchService productSearchService,
        ICategoryService categoryService,
        ILogger<ProductGrpcService> logger)
    {
        _productService = productService;
        _productSearchService = productSearchService;
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

    public override async Task<ProductResponse> CreateProduct(CreateProductRequest request, ServerCallContext context)
    {
       if (string.IsNullOrWhiteSpace(request.Name))
           throw new RpcException(new Status(StatusCode.InvalidArgument, "Product name is required."));
       if (request.BasePrice < 0 || request.Variants.Any(v => v.Price < 0 || v.Stock < 0))
           throw new RpcException(new Status(StatusCode.InvalidArgument, "Price and stock cannot be negative."));
       if (!Guid.TryParse(request.CategoryId, out var categoryId))
           throw new RpcException(new Status(StatusCode.InvalidArgument, "CategoryId is not a valid GUID."));

       var images = new List<CreateProductImageDto>();
       foreach (var i in request.Images)
       {
           if (!Guid.TryParse(i.ImageFileId, out var imageFileId))
               throw new RpcException(new Status(StatusCode.InvalidArgument, $"ImageFileId '{i.ImageFileId}' is not a valid GUID."));

           images.Add(new CreateProductImageDto
           {
               ImageFileId = imageFileId,
               Url = i.Url,
               AltText = i.AltText,
               IsPrimary = i.IsPrimary
           });
       }

       var dto = new CreateProductDto
       {
            Name = request.Name,
            Description = request.Description,
            BasePrice = (decimal)request.BasePrice,
            CategoryId = categoryId,
            Variants = request.Variants.Select(v => new CreateProductVariantDto
            {
                Name = v.Name,
                Price = v.Price == 0 ? null : (decimal)v.Price,
                Stock = v.Stock,
                SKU = v.Sku
            }).ToList(),
            Images = images
       };

       ProductResponseDto result;
       try
       {
           result = await _productService.CreateAsync(dto);
       }
       catch (KeyNotFoundException ex)
       {
           throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
       }

         return new ProductResponse
         {
                Id = result.Id.ToString(),
                Name = result.Name,
                Description = result.Description ?? "",
                BasePrice = (double)result.BasePrice,
                CategoryId = result.CategoryId.ToString()
         };
    }

    public override async Task<ProductDetailResponse> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Product ID is not a valid GUID."));

        var product = await _productService.GetByIdAsync(id)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.Id} was not found."));

        var response = new ProductDetailResponse
        {
            Id = product.Id.ToString(),
            Name = product.Name,
            Description = product.Description ?? "",
            BasePrice = (double)product.BasePrice,
            CategoryId = product.CategoryId.ToString(),
            IsActive = product.IsActive
        };

        response.Variants.AddRange(product.Variants.Select(v => new ProductVariantDetail
        {
            Id = v.Id.ToString(),
            Name = v.Name,
            Price = (double)v.Price,
            Stock = v.Stock,
            Sku = v.SKU ?? ""
        }));

        response.Images.AddRange(product.Images.Select(i => new ProductImageMessage
        {
            ImageFileId = i.ImageFileId.ToString(),
            Url = i.Url,
            AltText = i.AltText ?? "",
            IsPrimary = i.IsPrimary
        }));

        return response;
    }

    public override async Task<SearchProductsResponse> SearchProducts(SearchProductsRequest request, ServerCallContext context)
    {
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 10;

        var (products, totalCount) = await _productSearchService.SearchAsync(request.Query, page, pageSize);

        var response = new SearchProductsResponse
        {
            TotalCount = totalCount
        };

        foreach (var p in products)
        {
            response.Products.Add(new ProductResponse
            {
                Id = p.Id.ToString(),
                Name = p.Name,
                Description = p.Description ?? "",
                BasePrice = (double)p.BasePrice,
                CategoryId = p.CategoryId.ToString()
            });
        }

        return response;
    }
}
