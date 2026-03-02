namespace PhotoBase.Shared.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Metadata fields submitted alongside the image file during upload.
/// The actual file is sent as multipart form data; these fields ride along.
/// </summary>
public class AssetUploadRequest
{
    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? AccessionNumber { get; set; }

    [MaxLength(1000)]
    public string? Keywords { get; set; }

    [MaxLength(200)]
  public string? Location { get; set; }

    [MaxLength(150)]
    public string? Photographer { get; set; }

    public DateTime? DateTaken { get; set; }

    [MaxLength(500)]
    public string? Categories { get; set; }

    /// <summary>
    /// If true, allow upload even when a duplicate hash is detected.
    /// Default false — duplicate blocks the upload.
    /// </summary>
    public bool ForceOverrideDuplicate { get; set; }
}
