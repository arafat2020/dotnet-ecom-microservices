using gateway.Models;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Protos.Product;

namespace gateway.Controllers;

[Route("api/products")]
public class ProductController : ApiControllerBase
{
    private readonly ProductService.ProductServiceClient _productServiceClient;
    private readonly ILogger<ProductController> _logger;

    public ProductController(ProductService.ProductServiceClient productServiceClient, ILogger<ProductController> logger)
    {
        _productServiceClient = productServiceClient;
        _logger = logger;
    }

    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SearchProductsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var request = new SearchProductsRequest
            {
                Query = query ?? string.Empty,
                Page = page,
                PageSize = pageSize
            };

            var response = await _productServiceClient.SearchProductsAsync(request);

            return ApiOk(response, "Products retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Product Service for search via gRPC.");
            return ApiInternalError("An internal error occurred while searching products.");
        }
    }

    /// <summary>
    /// Creates a new product with its variants and images. Only accessible by Admin and SuperAdmin users.
    /// Upload the images first through <c>POST /api/images/upload</c> and pass the returned IDs and URLs.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequestDto request)
    {
        try
        {
            var grpcRequest = new CreateProductRequest
            {
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                BasePrice = (double)request.BasePrice,
                CategoryId = request.CategoryId
            };

            grpcRequest.Variants.AddRange(request.Variants.Select(v => new ProductVariantMessage
            {
                Name = v.Name,
                Price = (double)(v.Price ?? 0),
                Stock = v.Stock,
                Sku = v.Sku ?? string.Empty
            }));

            grpcRequest.Images.AddRange(request.Images.Select(i => new ProductImageMessage
            {
                ImageFileId = i.ImageFileId,
                Url = i.Url,
                AltText = i.AltText ?? string.Empty,
                IsPrimary = i.IsPrimary
            }));

            var response = await _productServiceClient.CreateProductAsync(grpcRequest);

            return ApiCreated(response, "Product created successfully.");
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex, "gRPC error creating product: {Status}", ex.StatusCode);
            return ApiRpcError(ex, "Product Service");
        }
    }

    /// <summary>
    /// Retrieves one product with its variants and images.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        try
        {
            var response = await _productServiceClient.GetProductAsync(new GetProductRequest { Id = id.ToString() });

            return ApiOk(response, "Product retrieved successfully.");
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex, "gRPC error retrieving product {Id}: {Status}", id, ex.StatusCode);
            return ApiRpcError(ex, "Product Service");
        }
    }
}
