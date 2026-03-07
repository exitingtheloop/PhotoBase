using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoBase.API.Configuration;
using PhotoBase.API.Data;
using PhotoBase.API.Entities;
using PhotoBase.API.Services;
using PhotoBase.Shared.Dtos;
using PhotoBase.Shared.Requests;
using PhotoBase.Shared.Responses;

namespace PhotoBase.API.Controllers;

[ApiController]
[Route("api/assets")]
public class AssetsController : ControllerBase
{
    private readonly PhotoBaseDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IThumbnailService _thumbs;
private readonly IHashService _hash;
    private readonly UploadOptions _uploadOpts;
    private readonly ILogger<AssetsController> _log;

    public AssetsController(
        PhotoBaseDbContext db,
      IFileStorage storage,
        IThumbnailService thumbs,
        IHashService hash,
        IOptions<UploadOptions> uploadOpts,
        ILogger<AssetsController> log)
    {
  _db = db;
        _storage = storage;
 _thumbs = thumbs;
    _hash = hash;
        _uploadOpts = uploadOpts.Value;
        _log = log;
    }

    // ???????????????????????????????????????????????
    // GET /api/assets?query=&page=1&pageSize=20
    // ???????????????????????????????????????????????
    [HttpGet]
    public async Task<ActionResult<PagedResult<AssetListItemDto>>> Search(
        [FromQuery] string? query,
      [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<ImageAsset> q = _db.ImageAssets
            .Include(a => a.PlantRecord)
    .AsNoTracking();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(query))
        {
       var term = query.Trim();
     q = q.Where(a =>
     a.Title.Contains(term) ||
    (a.AccessionNumber != null && a.AccessionNumber.Contains(term)) ||
     (a.PlantRecord != null && a.PlantRecord.PlantName.Contains(term)) ||
            (a.Keywords != null && a.Keywords.Contains(term)) ||
           (a.Location != null && a.Location.Contains(term)) ||
                (a.Categories != null && a.Categories.Contains(term))
   );
        }

        var totalCount = await q.CountAsync(ct);

        var items = await q
   .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
         .Take(pageSize)
            .Select(a => new AssetListItemDto
     {
             Id = a.Id,
 Title = a.Title,
         AccessionNumber = a.AccessionNumber,
      PlantName = a.PlantRecord != null ? a.PlantRecord.PlantName : null,
      Location = a.Location,
             Photographer = a.Photographer,
    Keywords = a.Keywords,
  Categories = a.Categories,
      DateTaken = a.DateTaken,
     CreatedAt = a.CreatedAt,
  ThumbUrl = $"/api/assets/{a.Id}/thumb"
   })
            .ToListAsync(ct);

      return Ok(new PagedResult<AssetListItemDto>
        {
     Items = items,
        Page = page,
     PageSize = pageSize,
     TotalCount = totalCount
        });
    }

    // ???????????????????????????????????????????????
    // GET /api/assets/{id}
    // ???????????????????????????????????????????????
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssetDetailDto>> GetDetail(Guid id, CancellationToken ct)
    {
        var asset = await _db.ImageAssets
            .Include(a => a.PlantRecord)
     .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

 if (asset is null)
return NotFound();

        return Ok(new AssetDetailDto
  {
            Id = asset.Id,
 Title = asset.Title,
            AccessionNumber = asset.AccessionNumber,
            PlantName = asset.PlantRecord?.PlantName,
            Location = asset.Location,
    Photographer = asset.Photographer,
         Keywords = asset.Keywords,
Categories = asset.Categories,
  DateTaken = asset.DateTaken,
    CreatedAt = asset.CreatedAt,
            OriginalFileName = asset.OriginalFileName,
       ThumbUrl = $"/api/assets/{asset.Id}/thumb"
     });
    }

    // ???????????????????????????????????????????????
  // GET /api/assets/{id}/thumb
    // ???????????????????????????????????????????????
    [HttpGet("{id:guid}/thumb")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetThumbnail(Guid id, CancellationToken ct)
    {
  var asset = await _db.ImageAssets
  .AsNoTracking()
 .Select(a => new { a.Id, a.ThumbPath })
        .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (asset is null)
        return NotFound();

        var fullPath = _storage.GetFullPath(asset.ThumbPath);
        if (!System.IO.File.Exists(fullPath))
        return NotFound();

      return PhysicalFile(fullPath, "image/jpeg");
  }

    // ???????????????????????????????????????????????
    // GET /api/assets/{id}/download
    // (No auth for now — will be gated later)
    // ???????????????????????????????????????????????
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var asset = await _db.ImageAssets
      .AsNoTracking()
     .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (asset is null)
      return NotFound();

   var fullPath = _storage.GetFullPath(asset.OriginalPath);
        if (!System.IO.File.Exists(fullPath))
     return NotFound();

        var contentType = ResolveContentType(fullPath);
        var downloadName = asset.OriginalFileName ?? Path.GetFileName(fullPath);

   return PhysicalFile(fullPath, contentType, downloadName);
    }

    // ???????????????????????????????????????????????
    // POST /api/assets
    // ???????????????????????????????????????????????
    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<AssetDetailDto>> Upload(
 IFormFile file,
   [FromForm] AssetUploadRequest request,
        CancellationToken ct)
    {
        // --- Validate file presence ---
        if (file is null || file.Length == 0)
      {
        return Problem(
                title: "No file provided",
                detail: "Please upload an image file.",
   statusCode: StatusCodes.Status400BadRequest);
    }

        // --- Validate file size ---
if (file.Length > _uploadOpts.MaxFileSizeBytes)
        {
     return Problem(
     title: "File too large",
                detail: $"Maximum file size is {_uploadOpts.MaxFileSizeBytes / (1024 * 1024)} MB.",
             statusCode: StatusCodes.Status400BadRequest);
        }

        // --- Validate file extension ---
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_uploadOpts.AllowedExtensions.Contains(ext))
        {
       return Problem(
  title: "Invalid file type",
       detail: $"Allowed types: {string.Join(", ", _uploadOpts.AllowedExtensions)}. Received: '{ext}'.",
    statusCode: StatusCodes.Status400BadRequest);
        }

   await using var fileStream = file.OpenReadStream();

        // --- Compute hash for duplicate detection (ADR-0005) ---
        var hash = await _hash.ComputeSha256Async(fileStream, ct);

        var existingByHash = await _db.ImageAssets
     .AsNoTracking()
   .FirstOrDefaultAsync(a => a.HashSha256 == hash, ct);

        if (existingByHash is not null && !request.ForceOverrideDuplicate)
    {
     return Problem(
                title: "Duplicate file detected",
        detail: $"An identical file already exists (asset {existingByHash.Id}, title: '{existingByHash.Title}'). " +
    "Set ForceOverrideDuplicate=true to upload anyway.",
        statusCode: StatusCodes.Status409Conflict);
        }

        // --- Look up PlantRecord for auto-fill ---
        PlantRecord? plant = null;
  if (!string.IsNullOrWhiteSpace(request.AccessionNumber))
        {
 plant = await _db.PlantRecords
              .FirstOrDefaultAsync(p => p.AccessionNumber == request.AccessionNumber, ct);
        }

        // --- Create entity ---
        var assetId = Guid.NewGuid();

        var asset = new ImageAsset
     {
     Id = assetId,
            Title = request.Title,
            AccessionNumber = request.AccessionNumber,
            Keywords = request.Keywords,
 Photographer = request.Photographer,
   DateTaken = request.DateTaken,
            Categories = request.Categories,
            OriginalFileName = file.FileName,
       HashSha256 = hash,
  CreatedAt = DateTime.UtcNow,
            // Auto-fill location from PlantRecord if not provided
            Location = !string.IsNullOrWhiteSpace(request.Location)
         ? request.Location
                : plant?.Location,
    };

        // --- Generate thumbnail ---
        using var thumbStream = await _thumbs.GenerateThumbnailAsync(fileStream, ct);
        asset.ThumbPath = await _storage.SaveThumbnailAsync(assetId, thumbStream, ct);

        // --- Save original ---
        asset.OriginalPath = await _storage.SaveOriginalAsync(assetId, file.FileName, fileStream, ct);

    // --- Persist ---
        _db.ImageAssets.Add(asset);
   await _db.SaveChangesAsync(ct);

        _log.LogInformation("Asset uploaded: {AssetId} '{Title}' (hash: {Hash})",
   assetId, asset.Title, hash[..12]);

   var dto = new AssetDetailDto
    {
     Id = asset.Id,
            Title = asset.Title,
 AccessionNumber = asset.AccessionNumber,
          PlantName = plant?.PlantName,
         Location = asset.Location,
            Photographer = asset.Photographer,
 Keywords = asset.Keywords,
         Categories = asset.Categories,
            DateTaken = asset.DateTaken,
            CreatedAt = asset.CreatedAt,
            OriginalFileName = asset.OriginalFileName,
     ThumbUrl = $"/api/assets/{asset.Id}/thumb"
        };

        return CreatedAtAction(nameof(GetDetail), new { id = asset.Id }, dto);
    }

 // ???????????????????????????????????????????????
    // Helpers
    // ???????????????????????????????????????????????
    private static string ResolveContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
     ".jpg" or ".jpeg" => "image/jpeg",
       ".png" => "image/png",
      ".tif" or ".tiff" => "image/tiff",
     ".bmp" => "image/bmp",
      ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
