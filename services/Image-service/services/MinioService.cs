using Amazon.S3;
using Amazon.S3.Model;
using Image_service.Db;
using Image_service.Model;

namespace Image_service.Services;

public interface IMinioService
{
    Task<FileMetadata> UploadFileAsync(IFormFile file, EntityType? entityType, string? entityId);
    Task DeleteFileAsync(Guid id);
    string GetPublicUrl(string bucket, string objectKey);
}

public class MinioService : IMinioService
{
    private readonly IAmazonS3 _s3;
    private readonly IImageResizeService _resizer;
    private readonly ImageDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<MinioService> _logger;

    public MinioService(
        IAmazonS3 s3,
        IImageResizeService resizer,
        ImageDbContext db,
        IConfiguration config,
        ILogger<MinioService> logger)
    {
        _s3 = s3;
        _resizer = resizer;
        _db = db;
        _config = config;
        _logger = logger;
    }

    // ─── Upload ────────────────────────────────────────────────────────────────

    public async Task<FileMetadata> UploadFileAsync(IFormFile file, EntityType? entityType, string? entityId)
    {
        var bucket = _config["Minio:BucketName"] ?? "images";
        await EnsureBucketExistsAsync(bucket);

        var id = Guid.NewGuid();
        var safeName = Path.GetFileName(file.FileName);

        // Keys
        var originalKey = $"{id}_original_{safeName}";
        var thumbKey     = $"{id}_thumb_{safeName}";

        // ── Upload original ──────────────────────────────────────────────────
        using var originalStream = file.OpenReadStream();
        await PutObjectAsync(bucket, originalKey, originalStream, file.ContentType, file.Length);

        // ── Upload thumbnail (images only) ──────────────────────────────────
        string? thumbnailObjectKey = null;
        string? thumbnailUrl       = null;

        // Reset stream position for resize
        using var resizeStream = file.OpenReadStream();
        var thumbStream = await _resizer.ResizeAsync(resizeStream, file.ContentType);
        if (thumbStream is not null)
        {
            await using (thumbStream)
            {
                await PutObjectAsync(bucket, thumbKey, thumbStream, file.ContentType, thumbStream.Length);
            }
            thumbnailObjectKey = thumbKey;
            thumbnailUrl       = GetPublicUrl(bucket, thumbKey);
        }

        // ── Persist metadata ─────────────────────────────────────────────────
        var metadata = new FileMetadata
        {
            Id                 = id,
            FileName           = safeName,
            Bucket             = bucket,
            ObjectKey          = originalKey,
            Url                = GetPublicUrl(bucket, originalKey),
            ThumbnailObjectKey = thumbnailObjectKey,
            ThumbnailUrl       = thumbnailUrl,
            ContentType        = file.ContentType,
            Size               = file.Length,
            EntityType         = entityType,
            EntityId           = entityId,
            CreatedAt          = DateTime.UtcNow
        };

        _db.Files.Add(metadata);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Uploaded file {FileName} as {ObjectKey}", safeName, originalKey);
        return metadata;
    }

    // ─── Delete ────────────────────────────────────────────────────────────────

    public async Task DeleteFileAsync(Guid id)
    {
        var meta = await _db.Files.FindAsync(id)
            ?? throw new KeyNotFoundException($"File {id} not found.");

        await DeleteObjectAsync(meta.Bucket, meta.ObjectKey);

        if (meta.ThumbnailObjectKey is not null)
            await DeleteObjectAsync(meta.Bucket, meta.ThumbnailObjectKey);

        _db.Files.Remove(meta);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted file {Id} ({ObjectKey})", id, meta.ObjectKey);
    }

    // ─── URL helper ────────────────────────────────────────────────────────────

    public string GetPublicUrl(string bucket, string objectKey)
    {
        var host = _config["Minio:PublicHost"]?.TrimEnd('/') ?? "http://localhost:9000";
        return $"{host}/{bucket}/{objectKey}";
    }

    // ─── Private helpers ───────────────────────────────────────────────────────

    private async Task EnsureBucketExistsAsync(string bucket)
    {
        try
        {
            await _s3.GetBucketLocationAsync(bucket);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await _s3.PutBucketAsync(bucket);
            _logger.LogInformation("Created MinIO bucket '{Bucket}'", bucket);

            // Make bucket publicly readable so URLs work from any origin
            var policy = $$"""
            {
              "Version": "2012-10-17",
              "Statement": [{
                "Effect": "Allow",
                "Principal": "*",
                "Action": "s3:GetObject",
                "Resource": "arn:aws:s3:::{{bucket}}/*"
              }]
            }
            """;
            await _s3.PutBucketPolicyAsync(new PutBucketPolicyRequest
            {
                BucketName = bucket,
                Policy     = policy
            });
        }
    }

    private async Task PutObjectAsync(string bucket, string key, Stream data, string contentType, long size)
    {
        var req = new PutObjectRequest
        {
            BucketName  = bucket,
            Key         = key,
            InputStream = data,
            ContentType = contentType,
            Headers     = { ContentLength = size }
        };
        await _s3.PutObjectAsync(req);
    }

    private async Task DeleteObjectAsync(string bucket, string key)
    {
        await _s3.DeleteObjectAsync(bucket, key);
    }
}
