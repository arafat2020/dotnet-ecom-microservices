using System.Threading.Channels;

namespace Image_service.Services;

public interface IImageDeletionQueue
{
    ValueTask QueueImageDeletionAsync(Guid imageId, CancellationToken cancellationToken = default);
    ValueTask<Guid> DequeueImageDeletionAsync(CancellationToken cancellationToken = default);
}

public class ImageDeletionQueue : IImageDeletionQueue
{
    private readonly Channel<Guid> _queue;

    public ImageDeletionQueue()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<Guid>(options);
    }

    public async ValueTask QueueImageDeletionAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        await _queue.Writer.WriteAsync(imageId, cancellationToken);
    }

    public async ValueTask<Guid> DequeueImageDeletionAsync(CancellationToken cancellationToken = default)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
