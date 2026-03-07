using System.Net;
using System.Net.Http.Json;
using PhotoBase.Shared.Dtos;
using PhotoBase.Shared.Responses;

namespace PhotoBase.Client.Services;

/// <summary>
/// Typed HTTP client for PhotoBase API endpoints.
/// Auth token is auto-attached by <see cref="AuthTokenHandler"/>.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http) => _http = http;

    // ── Assets (public) ─────────────────────────────────

    /// <summary>Search / list assets with paging, filters, and sort.</summary>
    public async Task<PagedResult<AssetListItemDto>> SearchAssetsAsync(
        string? query = null, int page = 1, int pageSize = 20,
        string? category = null, string? location = null,
        string? photographer = null, int? year = null,
        string sort = "newest")
    {
        var parts = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}",
            $"sort={Uri.EscapeDataString(sort)}"
        };
        if (!string.IsNullOrWhiteSpace(query))
            parts.Add($"query={Uri.EscapeDataString(query)}");
        if (!string.IsNullOrWhiteSpace(category))
            parts.Add($"category={Uri.EscapeDataString(category)}");
        if (!string.IsNullOrWhiteSpace(location))
            parts.Add($"location={Uri.EscapeDataString(location)}");
        if (!string.IsNullOrWhiteSpace(photographer))
            parts.Add($"photographer={Uri.EscapeDataString(photographer)}");
        if (year.HasValue)
            parts.Add($"year={year.Value}");

        var url = $"api/assets?{string.Join("&", parts)}";

        return await _http.GetFromJsonAsync<PagedResult<AssetListItemDto>>(url)
               ?? new PagedResult<AssetListItemDto>();
    }

    /// <summary>Get facet values (distinct categories, locations, photographers, years) with counts.</summary>
    public async Task<FacetsDto> GetFacetsAsync()
    {
        return await _http.GetFromJsonAsync<FacetsDto>("api/assets/facets")
               ?? new FacetsDto();
    }

    /// <summary>Get full detail for a single asset.</summary>
    public async Task<AssetDetailDto?> GetAssetAsync(Guid id)
    {
        try
        {
            return await _http.GetFromJsonAsync<AssetDetailDto>($"api/assets/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>Build the thumbnail URL for an asset (no HTTP call needed).</summary>
    public static string GetThumbUrl(Guid assetId) => $"api/assets/{assetId}/thumb";

    /// <summary>Build the download URL for an asset.</summary>
    public static string GetDownloadUrl(Guid assetId) => $"api/assets/{assetId}/download";

    // ── Download (auth required) ────────────────────────

    /// <summary>
    /// Download the original file as a byte array with auth header attached.
    /// Returns (bytes, fileName, contentType) or throws on failure.
    /// </summary>
    public async Task<(byte[] Bytes, string FileName, string ContentType)> DownloadOriginalAsync(Guid assetId)
    {
        var response = await _http.GetAsync($"api/assets/{assetId}/download");
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                   ?? $"download-{assetId}";
        var contentType = response.Content.Headers.ContentType?.MediaType
       ?? "application/octet-stream";

        return (bytes, fileName, contentType);
    }

    // ── Upload (admin) ──────────────────────────────────

    /// <summary>
    /// Upload an image with metadata. Returns the API response (may be error).
    /// </summary>
    public async Task<HttpResponseMessage> UploadAssetAsync(
        Stream fileStream, string fileName, string title,
        string? accessionNumber = null, string? keywords = null,
        string? location = null, string? photographer = null,
     DateTime? dateTaken = null, string? categories = null,
        bool forceOverrideDuplicate = false)
    {
        using var form = new MultipartFormDataContent();

        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "file", fileName);

        form.Add(new StringContent(title), "Title");
        if (!string.IsNullOrWhiteSpace(accessionNumber))
            form.Add(new StringContent(accessionNumber), "AccessionNumber");
        if (!string.IsNullOrWhiteSpace(keywords))
            form.Add(new StringContent(keywords), "Keywords");
        if (!string.IsNullOrWhiteSpace(location))
            form.Add(new StringContent(location), "Location");
        if (!string.IsNullOrWhiteSpace(photographer))
            form.Add(new StringContent(photographer), "Photographer");
        if (dateTaken.HasValue)
            form.Add(new StringContent(dateTaken.Value.ToString("o")), "DateTaken");
        if (!string.IsNullOrWhiteSpace(categories))
            form.Add(new StringContent(categories), "Categories");
        if (forceOverrideDuplicate)
            form.Add(new StringContent("true"), "ForceOverrideDuplicate");

        return await _http.PostAsync("api/assets", form);
    }

    // ── Import (admin) ──────────────────────────────────

    /// <summary>
    /// Import plant records from a TSV file. Returns the API response.
    /// </summary>
    public async Task<HttpResponseMessage> ImportPlantRecordsAsync(Stream fileStream, string fileName)
    {
        using var form = new MultipartFormDataContent();

        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/tab-separated-values");
        form.Add(fileContent, "file", fileName);

        return await _http.PostAsync("api/import/plant-records", form);
    }
}
