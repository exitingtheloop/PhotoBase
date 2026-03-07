using System.Net.Http.Json;
using PhotoBase.Shared.Dtos;
using PhotoBase.Shared.Responses;

namespace PhotoBase.Client.Services;

/// <summary>
/// Typed HTTP client for PhotoBase API endpoints.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http) => _http = http;

    // ?? Assets ??????????????????????????????????????????

    /// <summary>Search / list assets with paging.</summary>
    public async Task<PagedResult<AssetListItemDto>> SearchAssetsAsync(
        string? query = null, int page = 1, int pageSize = 20)
    {
        var url = $"api/assets?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(query))
            url += $"&query={Uri.EscapeDataString(query)}";

        return await _http.GetFromJsonAsync<PagedResult<AssetListItemDto>>(url)
       ?? new PagedResult<AssetListItemDto>();
    }

    /// <summary>Get full detail for a single asset.</summary>
    public async Task<AssetDetailDto?> GetAssetAsync(Guid id)
    {
        try
        {
            return await _http.GetFromJsonAsync<AssetDetailDto>($"api/assets/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>Build the thumbnail URL for an asset (no HTTP call needed).</summary>
    public static string GetThumbUrl(Guid assetId) => $"api/assets/{assetId}/thumb";

    /// <summary>Build the download URL for an asset.</summary>
    public static string GetDownloadUrl(Guid assetId) => $"api/assets/{assetId}/download";
}
