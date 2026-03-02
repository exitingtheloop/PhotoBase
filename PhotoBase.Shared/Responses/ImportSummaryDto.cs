namespace PhotoBase.Shared.Responses;

/// <summary>
/// Summary returned after a TSV plant record import.
/// </summary>
public class ImportSummaryDto
{
    public int Imported { get; set; }
    public int Updated { get; set; }
    public int Errors { get; set; }
    public List<string> ErrorDetails { get; set; } = new();
}
