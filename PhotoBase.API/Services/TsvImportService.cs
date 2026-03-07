using Microsoft.EntityFrameworkCore;
using PhotoBase.API.Data;
using PhotoBase.API.Entities;
using PhotoBase.Shared.Responses;

namespace PhotoBase.API.Services;

/// <summary>
/// Parses tab-separated plant record files and upserts into the database (ADR-0010).
/// </summary>
public class TsvImportService
{
    private readonly PhotoBaseDbContext _db;
    private readonly ILogger<TsvImportService> _log;

    // Required column names (case-insensitive match)
    private static readonly HashSet<string> RequiredColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "AccessionNumber", "PlantName", "Location"
    };

    public TsvImportService(PhotoBaseDbContext db, ILogger<TsvImportService> log)
    {
        _db = db;
        _log = log;
    }

    /// <summary>
    /// Parse the TSV stream, validate rows, and upsert PlantRecords by AccessionNumber.
    /// </summary>
    public async Task<ImportSummaryDto> ImportAsync(Stream tsvStream, CancellationToken ct = default)
    {
        var summary = new ImportSummaryDto();

        using var reader = new StreamReader(tsvStream);

        // --- Read header line ---
        var headerLine = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            summary.Errors = 1;
            summary.ErrorDetails.Add("File is empty or missing a header row.");
            return summary;
        }

        var headers = headerLine.Split('\t').Select(h => h.Trim()).ToArray();
        var columnMap = BuildColumnMap(headers);

        // Validate required columns exist
        foreach (var required in RequiredColumns)
        {
            if (!columnMap.ContainsKey(required))
            {
                summary.Errors = 1;
                summary.ErrorDetails.Add($"Missing required column: '{required}'. Found columns: [{string.Join(", ", headers)}]");
                return summary;
            }
        }

        // --- Read data rows ---
        var lineNumber = 1; // header was line 1
        while (!reader.EndOfStream)
        {
            lineNumber++;
            var line = await reader.ReadLineAsync(ct);

            if (string.IsNullOrWhiteSpace(line))
                continue; // skip blank lines

            var fields = line.Split('\t');

            // Parse fields by column index
            var accession = GetField(fields, columnMap, "AccessionNumber");
            var plantName = GetField(fields, columnMap, "PlantName");
            var location = GetField(fields, columnMap, "Location");
            var collectionTrip = GetField(fields, columnMap, "CollectionTrip");

            // Validate required values
            var rowErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(accession))
                rowErrors.Add("AccessionNumber is empty");
            if (string.IsNullOrWhiteSpace(plantName))
                rowErrors.Add("PlantName is empty");
            if (string.IsNullOrWhiteSpace(location))
                rowErrors.Add("Location is empty");

            if (rowErrors.Count > 0)
            {
                summary.Errors++;
                summary.ErrorDetails.Add($"Line {lineNumber}: {string.Join("; ", rowErrors)}");
                continue;
            }

            // Upsert
            try
            {
                var existing = await _db.PlantRecords
    .FirstOrDefaultAsync(p => p.AccessionNumber == accession, ct);

                if (existing is not null)
                {
                    existing.PlantName = plantName!;
                    existing.Location = location;
                    existing.CollectionTrip = collectionTrip;
                    existing.UpdatedAt = DateTime.UtcNow;
                    summary.Updated++;
                }
                else
                {
                    _db.PlantRecords.Add(new PlantRecord
                    {
                        AccessionNumber = accession!,
                        PlantName = plantName!,
                        Location = location,
                        CollectionTrip = collectionTrip,
                        UpdatedAt = DateTime.UtcNow
                    });
                    summary.Imported++;
                }
            }
            catch (Exception ex)
            {
                summary.Errors++;
                summary.ErrorDetails.Add($"Line {lineNumber}: {ex.Message}");
            }
        }

        // Save all changes in one batch
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to save plant record import batch");
            summary.ErrorDetails.Add($"Database save failed: {ex.Message}");
            summary.Errors++;
        }

        _log.LogInformation(
            "TSV import complete: Imported={Imported}, Updated={Updated}, Errors={Errors}",
            summary.Imported, summary.Updated, summary.Errors);

        return summary;
    }

    /// <summary>
    /// Build a case-insensitive map from column name ? array index.
    /// </summary>
    private static Dictionary<string, int> BuildColumnMap(string[] headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
        {
            var name = headers[i].Trim();
            if (!string.IsNullOrWhiteSpace(name) && !map.ContainsKey(name))
                map[name] = i;
        }
        return map;
    }

    /// <summary>
    /// Safely get a trimmed field value by column name; returns null if column missing or index out of range.
    /// </summary>
    private static string? GetField(string[] fields, Dictionary<string, int> columnMap, string columnName)
    {
        if (!columnMap.TryGetValue(columnName, out var idx) || idx >= fields.Length)
            return null;

        var value = fields[idx].Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
