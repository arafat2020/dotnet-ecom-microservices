using Image_service.Db;
using Image_service.Model;
using Image_service.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Image_service.Controller;

[ApiController]
[Route("api/images")]
public class ImageController : ControllerBase
{
    private readonly IMinioService _minio;
    private readonly ImageDbContext _db;
    private readonly ILogger<ImageController> _logger;

    public ImageController(IMinioService minio, ImageDbContext db, ILogger<ImageController> logger)
    {
        _minio  = minio;
        _db     = db;
        _logger = logger;
    }

    // POST /api/images/upload
    [HttpPost("upload")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB max
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromQuery] EntityType? entityType = null,
        [FromQuery] string? entityId       = null)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        var metadata = await _minio.UploadFileAsync(file, entityType, entityId);
        return CreatedAtAction(nameof(GetById), new { id = metadata.Id }, metadata);
    }

    // GET /api/images/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var meta = await _db.Files.FindAsync(id);
        return meta is null ? NotFound() : Ok(meta);
    }

    // GET /api/images?entityType=product&entityId=123
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] EntityType? entityType = null,
        [FromQuery] string? entityId       = null)
    {
        var query = _db.Files.AsQueryable();

        if (entityType.HasValue)
            query = query.Where(f => f.EntityType == entityType);

        if (!string.IsNullOrEmpty(entityId))
            query = query.Where(f => f.EntityId == entityId);

        var results = await query
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return Ok(results);
    }

    // DELETE /api/images/{id}
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _minio.DeleteFileAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
