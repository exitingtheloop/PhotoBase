namespace PhotoBase.API.Entities;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Plant record imported via TSV, keyed by accession number.
/// </summary>
public class PlantRecord
{
    [Key]
    [MaxLength(50)]
    public string AccessionNumber { get; set; } = string.Empty;

[MaxLength(200)]
    public string PlantName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(200)]
    public string? CollectionTrip { get; set; }

 public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

 // Navigation: images linked to this plant record
    public ICollection<ImageAsset> ImageAssets { get; set; } = new List<ImageAsset>();
}
