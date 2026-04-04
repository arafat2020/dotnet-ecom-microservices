using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Protos.Product;

namespace gateway.Controllers;

/// <summary>
/// Controller for Category operations.
/// Acts as a REST-to-gRPC proxy for the Product Service.
/// </summary>
[ApiController]
[Route("api/categories")]
public class CategoryController : ControllerBase
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
    /// <returns>The created category details.</returns>
    /// <response code="200">Returns the created category.</response>
    /// <response code="401">Unauthorized if no valid token is provided.</response>
    /// <response code="403">Forbidden if the user is not an Admin.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CreateCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        try
        {
            var response = await _productServiceClient.CreateCategoryAsync(request);
            if (!response.Success)
            {
                return BadRequest(new { response.Message });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Product Service via gRPC.");
            return StatusCode(500, "An internal error occurred while communicating with the Product Service.");
        }
    }
}
