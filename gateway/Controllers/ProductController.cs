using gateway.Models;
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
}
