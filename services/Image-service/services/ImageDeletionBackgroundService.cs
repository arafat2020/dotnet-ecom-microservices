namespace Image_service.Services;

public class ImageDeletionBackgroundService : BackgroundService
{
    private readonly IImageDeletionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImageDeletionBackgroundService> _logger;

    public ImageDeletionBackgroundService(
        IImageDeletionQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<ImageDeletionBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Image Deletion Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var imageId = await _queue.DequeueImageDeletionAsync(stoppingToken);

                await using var scope = _scopeFactory.CreateAsyncScope();
                var minioService = scope.ServiceProvider.GetRequiredService<IMinioService>();

                try
                {
                    await minioService.DeleteFileAsync(imageId);
                    _logger.LogInformation("Successfully deleted image {ImageId} in background.", imageId);
                }
                catch (KeyNotFoundException)
                {
                    _logger.LogWarning("Image {ImageId} not found during background deletion.", imageId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while deleting image {ImageId} in background.", imageId);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing image deletion queue.");
            }
        }

        _logger.LogInformation("Image Deletion Background Service is stopping.");
    }
}
