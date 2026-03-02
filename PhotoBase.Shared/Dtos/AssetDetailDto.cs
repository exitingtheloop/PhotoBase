namespace PhotoBase.Shared.Dtos;

/// <summary>
/// Full detail DTO for a single image asset (public detail view).
/// Does NOT include original file path — that's only accessible via download endpoint.
/// </summary>
public class AssetDetailDto
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
    public string? OriginalFileName { get; set; }
    public string ThumbUrl { get; set; } = string.Empty;
}
