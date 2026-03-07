using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace PhotoBase.API.Services;

public class ImageSharpThumbnailService : IThumbnailService
{
    private const int MaxDimension = 512;
    private const int JpegQuality = 80;

    public async Task<Stream> GenerateThumbnailAsync(Stream sourceImage, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceImage);

        if (sourceImage.CanSeek)
            sourceImage.Position = 0;

        using var image = await Image.LoadAsync(sourceImage, ct);

        // Resize so the longest edge is MaxDimension, preserving aspect ratio
        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(MaxDimension, MaxDimension)
        }));

        var output = new MemoryStream();
        await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = JpegQuality }, ct);
        output.Position = 0;

        // Reset source so callers can still read it (e.g. to save original)
        if (sourceImage.CanSeek)
            sourceImage.Position = 0;

        return output;
    }
}
