namespace PhotoBase.API.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// An uploaded image asset with metadata, thumbnail, and duplicate-detection hash.
/// </summary>
public class ImageAsset
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Relative path to the original file within the storage root.</summary>
    [MaxLength(500)]
    public string OriginalPath { get; set; } = string.Empty;

    /// <summary>Relative path to the generated thumbnail.</summary>
    [MaxLength(500)]
    public string ThumbPath { get; set; } = string.Empty;

    /// <summary>SHA-256 hash of the original file bytes (hex, lowercase). Unique index.</summary>
    [MaxLength(64)]
    public string HashSha256 { get; set; } = string.Empty;

    // ---- Optional FK to PlantRecord ----
    [MaxLength(50)]
    public string? AccessionNumber { get; set; }

    [ForeignKey(nameof(AccessionNumber))]
    public PlantRecord? PlantRecord { get; set; }

// ---- Metadata fields ----
  [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Comma-separated keywords for search.</summary>
    [MaxLength(1000)]
    public string? Keywords { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(150)]
    public string? Photographer { get; set; }

    public DateTime? DateTaken { get; set; }

    /// <summary>Comma-separated category labels.</summary>
    [MaxLength(500)]
    public string? Categories { get; set; }

    /// <summary>Original file name as uploaded (for display only — never used for storage paths).</summary>
    [MaxLength(260)]
    public string? OriginalFileName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
