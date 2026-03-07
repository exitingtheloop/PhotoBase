using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoBase.API.Services;
using PhotoBase.Shared.Constants;
using PhotoBase.Shared.Responses;

namespace PhotoBase.API.Controllers;

[ApiController]
[Route("api/import")]
[Authorize(Roles = AppRoles.Admin)]
public class ImportController : ControllerBase
{
    private readonly TsvImportService _importService;

    public ImportController(TsvImportService importService)
    {
        _importService = importService;
    }

    /// <summary>
    /// Import plant records from a tab-separated file.
    /// Upserts by AccessionNumber. Returns import summary with counts and line-level errors.
    /// </summary>
    [HttpPost("plant-records")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB max for TSV
    public async Task<ActionResult<ImportSummaryDto>> ImportPlantRecords(
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return Problem(
       title: "No file provided",
    detail: "Please upload a .tsv or .txt file containing plant records.",
    statusCode: StatusCodes.Status400BadRequest);
        }

        // Basic extension check
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".tsv" and not ".txt" and not ".tab")
        {
            return Problem(
               title: "Invalid file type",
                  detail: $"Expected a .tsv, .txt, or .tab file, but received '{ext}'.",
          statusCode: StatusCodes.Status400BadRequest);
        }

        await using var stream = file.OpenReadStream();
        var summary = await _importService.ImportAsync(stream, ct);

        // Return 200 even with partial errors (the summary contains the detail)
        return Ok(summary);
    }
}
