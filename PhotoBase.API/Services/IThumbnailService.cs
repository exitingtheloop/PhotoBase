namespace PhotoBase.API.Services;

/// <summary>
/// Generates thumbnails from image streams (ADR-0004).
/// </summary>
public interface IThumbnailService
{
    /// <summary>
    /// Generate a JPEG thumbnail from the source image stream.
    /// Returns a seekable stream containing the thumbnail bytes.
    /// Caller is responsible for disposing the returned stream.
    /// </summary>
    Task<Stream> GenerateThumbnailAsync(Stream sourceImage, CancellationToken ct = default);
}
