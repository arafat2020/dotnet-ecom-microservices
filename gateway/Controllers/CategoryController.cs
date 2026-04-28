using gateway.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Protos.Product;

namespace gateway.Controllers;

/// <summary>
/// Controller for Category operations.
/// Acts as a REST-to-gRPC proxy for the Product Service.
/// All responses are wrapped in a standardized <see cref="ApiResponse{T}"/> envelope.
/// </summary>
[Route("api/categories")]
public class CategoryController : ApiControllerBase
{
    private readonly ProductService.ProductServiceClient _productServiceClient;
    private readonly ILogger<CategoryController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryController"/> class.
    /// </summary>
    /// <param name="productServiceClient">The gRPC client for the Product Service.</param>
    /// <param name="logger">The logger instance.</param>
    public CategoryController(ProductService.ProductServiceClient productServiceClient, ILogger<CategoryController> logger)
    {
        _productServiceClient = productServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new category. Only accessible by users with the Admin role.
    /// </summary>
    /// <param name="request">The category details.</param>
    /// <returns>A standardized response containing the created category details.</returns>
    /// <response code="200">Returns the created category.</response>
    /// <response code="400">Request was rejected by the Product Service.</response>
    /// <response code="401">No valid token was provided.</response>
    /// <response code="403">The authenticated user is not an Admin.</response>
    /// <response code="500">Product Service is unavailable.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<CreateCategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        try
        {
            var response = await _productServiceClient.CreateCategoryAsync(request);

            if (!response.Success)
            {
                return ApiBadRequest(response.Message);
            }

            return ApiOk(response, "Category created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Product Service via gRPC.");
            return ApiInternalError("An internal error occurred while communicating with the Product Service.");
        }
    }

    /// <summary>
    /// Retrieves all categories.
    /// </summary>
    /// <returns>A standardized response containing the list of categories.</returns>
    /// <response code="200">Returns the list of categories.</response>
    /// <response code="500">Product Service is unavailable.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<GetCategoriesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var response = await _productServiceClient.GetCategoriesAsync(new GetCategoriesRequest());

            if (!response.Success)
            {
                return ApiInternalError(response.Message);
            }

            return ApiOk(response, "Categories retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Product Service via gRPC.");
            return ApiInternalError("An internal error occurred while communicating with the Product Service.");
        }
    }
}
