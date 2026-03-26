using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Image_service.Services;

public interface IImageResizeService
{
    /// <summary>
    /// Resizes the provided image stream to fit within <paramref name="maxWidth"/> x <paramref name="maxHeight"/>
    /// while preserving the aspect ratio. Returns a new MemoryStream containing the resized image.
    /// Returns null if the content type is not an image.
    /// </summary>
    Task<MemoryStream?> ResizeAsync(Stream source, string contentType, int maxWidth = 300, int maxHeight = 300);
}

public class ImageResizeService : IImageResizeService
{
    private static readonly HashSet<string> SupportedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp", "image/tiff"
    };

    public async Task<MemoryStream?> ResizeAsync(Stream source, string contentType, int maxWidth = 300, int maxHeight = 300)
    {
        if (!SupportedImageTypes.Contains(contentType))
            return null;

        // Load the image from the source stream
        using var image = await Image.LoadAsync(source);

        // Preserve aspect ratio — shrink only if larger than target
        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(maxWidth, maxHeight),
            Mode = ResizeMode.Max
        }));

        var output = new MemoryStream();
        // Re-encode in the same format as the original
        await image.SaveAsync(output, image.Metadata.DecodedImageFormat!);
        output.Position = 0;

        return output;
    }
}
