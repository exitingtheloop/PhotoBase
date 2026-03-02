namespace PhotoBase.Shared.Dtos;

/// <summary>
/// Lightweight DTO returned in search/gallery list views.
/// </summary>
public class AssetListItemDto
{
  public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? AccessionNumber { get; set; }
    public string? PlantName { get; set; }
    public string? Location { get; set; }
    public string? Photographer { get; set; }
    public string? Keywords { get; set; }
    public string? Categories { get; set; }
  public DateTime? DateTaken { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Relative URL to thumbnail endpoint, e.g. /api/assets/{id}/thumb</summary>
    public string ThumbUrl { get; set; } = string.Empty;
}
