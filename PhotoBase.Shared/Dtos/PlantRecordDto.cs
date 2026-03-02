namespace PhotoBase.Shared.Dtos;

/// <summary>
/// DTO for plant record data (returned from API, used in Client for auto-fill).
/// </summary>
public class PlantRecordDto
{
    public string AccessionNumber { get; set; } = string.Empty;
    public string PlantName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? CollectionTrip { get; set; }
    public DateTime UpdatedAt { get; set; }
}
