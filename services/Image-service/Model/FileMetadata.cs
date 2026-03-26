using System.ComponentModel.DataAnnotations;

namespace Image_service.Model;

public class FileMetadata
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string FileName { get; set; } = null!;

    [Required]
    public string Bucket { get; set; } = null!;

    /// <summary>Path of the original file inside the bucket.</summary>
    [Required]
    public string ObjectKey { get; set; } = null!;

    /// <summary>Public URL of the original file served from MinIO.</summary>
    [Required]
    public string Url { get; set; } = null!;

    /// <summary>Path of the resized thumbnail inside the bucket. Null for non-image files.</summary>
    public string? ThumbnailObjectKey { get; set; }

    /// <summary>Public URL of the thumbnail. Null for non-image files.</summary>
    public string? ThumbnailUrl { get; set; }

    [Required]
    public string ContentType { get; set; } = null!;

    /// <summary>File size in bytes (original).</summary>
    public long Size { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Owning entity type, e.g. Product, User. Set by the caller.</summary>
    public EntityType? EntityType { get; set; }

    /// <summary>ID of the owning entity in its respective service.</summary>
    public string? EntityId { get; set; }
}
