namespace PhotoBase.API.Configuration;

/// <summary>
/// Configurable storage paths, bound from appsettings "Storage" section.
/// </summary>
public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Base directory for all file storage (originals + thumbs).</summary>
    public string BasePath { get; set; } = "./data";

    /// <summary>Subfolder under BasePath for original uploads.</summary>
    public string OriginalsFolder { get; set; } = "originals";

    /// <summary>Subfolder under BasePath for generated thumbnails.</summary>
    public string ThumbsFolder { get; set; } = "thumbs";
}
