namespace PhotoBase.API.Configuration;

/// <summary>
/// Configurable upload constraints, bound from appsettings "Upload" section.
/// </summary>
public class UploadOptions
{
    public const string SectionName = "Upload";

    /// <summary>Maximum file size in bytes. Default 50 MB.</summary>
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>Allowed file extensions (lowercase, with dot). Default common image types.</summary>
    public string[] AllowedExtensions { get; set; } =
    [
        ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".bmp", ".webp"
    ];
}
