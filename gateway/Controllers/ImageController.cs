using gateway.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace gateway.Controllers;

/// <summary>
/// Controller for image operations.
/// Proxies requests to the REST API of the Image Service, so clients never call it directly.
/// </summary>
[Route("api/images")]
public class ImageController : ApiControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ImageController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageController"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory providing the "ImageService" HttpClient.</param>
    /// <param name="logger">The logger instance.</param>
    public ImageController(IHttpClientFactory httpClientFactory, ILogger<ImageController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a file (multipart form field <c>file</c>). Only accessible by Admin and SuperAdmin users.
    /// The returned <c>id</c> and <c>url</c> are what the product creation endpoint expects.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <param name="entityType">Optional owning entity type, e.g. "Product".</param>
    /// <param name="entityId">Optional ID of the owning entity.</param>
    /// <response code="201">Returns the metadata of the stored file.</response>
    /// <response code="400">No file was provided, or the Image Service rejected it.</response>
    /// <response code="401">No valid token was provided.</response>
    /// <response code="403">The authenticated user is not an Admin.</response>
    /// <response code="500">Image Service is unavailable.</response>
    [HttpPost("upload")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [RequestSizeLimit(100 * 1024 * 1024)] // same limit as the Image Service
    [ProducesResponseType(typeof(ApiResponse<JsonElement>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upload(
        IFormFile? file,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null)
    {
        if (file is null || file.Length == 0)
        {
            return ApiBadRequest("No file provided.");
        }

        var query = new Dictionary<string, string?>();
        if (!string.IsNullOrEmpty(entityType)) query["entityType"] = entityType;
        if (!string.IsNullOrEmpty(entityId)) query["entityId"] = entityId;

        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        if (MediaTypeHeaderValue.TryParse(file.ContentType, out var contentType))
        {
            fileContent.Headers.ContentType = contentType;
        }

        using var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", Path.GetFileName(file.FileName));

        return await SendAsync(
            client => client.PostAsync(QueryHelpers.AddQueryString("api/images/upload", query), form),
            "File uploaded successfully.");
    }

    /// <summary>
    /// Retrieves the metadata (URLs, size, content type) of an uploaded file.
    /// </summary>
    /// <param name="id">The unique file ID.</param>
    /// <response code="200">Returns the file metadata.</response>
    /// <response code="404">File not found.</response>
    /// <response code="500">Image Service is unavailable.</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<JsonElement>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetById([FromRoute] Guid id) =>
        SendAsync(client => client.GetAsync($"api/images/{id}"), "File metadata retrieved successfully.");

    /// <summary>
    /// Deletes an uploaded file and its thumbnail. Only accessible by Admin and SuperAdmin users.
    /// </summary>
    /// <param name="id">The unique file ID.</param>
    /// <response code="200">The file was deleted.</response>
    /// <response code="401">No valid token was provided.</response>
    /// <response code="403">The authenticated user is not an Admin.</response>
    /// <response code="404">File not found.</response>
    /// <response code="500">Image Service is unavailable.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Delete([FromRoute] Guid id) =>
        SendAsync(client => client.DeleteAsync($"api/images/{id}"), "File deleted successfully.");

    /// <summary>
    /// Calls the Image Service and wraps its answer in the standard envelope.
    /// </summary>
    private async Task<IActionResult> SendAsync(Func<HttpClient, Task<HttpResponseMessage>> send, string successMessage)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ImageService");
            using var response = await send(client);
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                if (string.IsNullOrWhiteSpace(body))
                {
                    return Ok(ApiResponse.OkEmpty(successMessage));
                }

                var data = JsonSerializer.Deserialize<JsonElement>(body);
                return response.StatusCode == HttpStatusCode.Created
                    ? ApiCreated(data, successMessage)
                    : ApiOk(data, successMessage);
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    return ApiNotFound("File not found.");
                case HttpStatusCode.BadRequest:
                    return ApiBadRequest("The Image Service rejected the request.");
                default:
                    _logger.LogError("Image Service returned {StatusCode}: {Body}", (int)response.StatusCode, body);
                    return ApiInternalError("Image Service returned an error.");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "Error calling the Image Service.");
            return ApiInternalError("Image Service communication error.");
        }
    }
}
