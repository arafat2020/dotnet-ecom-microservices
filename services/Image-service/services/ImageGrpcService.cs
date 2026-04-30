using Grpc.Core;
using Shared.Protos.Image;

namespace Image_service.Services;

public class ImageGrpcService : ImageService.ImageServiceBase
{
    private readonly IMinioService _minioService;
    private readonly IImageDeletionQueue _queue;
    private readonly ILogger<ImageGrpcService> _logger;

    public ImageGrpcService(
        IMinioService minioService,
        IImageDeletionQueue queue,
        ILogger<ImageGrpcService> logger)
    {
        _minioService = minioService;
        _queue = queue;
        _logger = logger;
    }

    public override async Task<DeleteImageResponse> DeleteImage(DeleteImageRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.ImageId, out var id))
        {
            return new DeleteImageResponse { Success = false, Message = "Invalid Image ID format." };
        }

        try
        {
            await _minioService.DeleteFileAsync(id);
            return new DeleteImageResponse { Success = true, Message = "Image deleted successfully." };
        }
        catch (KeyNotFoundException)
        {
            return new DeleteImageResponse { Success = false, Message = "Image not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image {ImageId} via gRPC.", id);
            return new DeleteImageResponse { Success = false, Message = "An error occurred while deleting the image." };
        }
    }

    public override async Task<BulkDeleteImageResponse> BulkDeleteImage(BulkDeleteImageRequest request, ServerCallContext context)
    {
        int queuedCount = 0;
        foreach (var stringId in request.ImageIds)
        {
            if (Guid.TryParse(stringId, out var id))
            {
                await _queue.QueueImageDeletionAsync(id, context.CancellationToken);
                queuedCount++;
            }
            else
            {
                _logger.LogWarning("Invalid image ID received in bulk delete request: {StringId}", stringId);
            }
        }

        return new BulkDeleteImageResponse
        {
            Success = true,
            Message = $"Queued {queuedCount} images for deletion."
        };
    }
}
