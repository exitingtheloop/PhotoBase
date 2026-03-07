namespace PhotoBase.API.Services;

/// <summary>
/// Abstraction over file storage (ADR-0003). MVP: local disk. Later: Azure Blob / S3.
/// </summary>
public interface IFileStorage
{
    /// <summary>Save a file and return the relative path within storage root.</summary>
    Task<string> SaveOriginalAsync(Guid assetId, string safeFileName, Stream content, CancellationToken ct = default);

    /// <summary>Save a thumbnail and return the relative path within storage root.</summary>
    Task<string> SaveThumbnailAsync(Guid assetId, Stream content, CancellationToken ct = default);

    /// <summary>Get the full filesystem path for a relative storage path.</summary>
    string GetFullPath(string relativePath);

    /// <summary>Delete both original and thumb files for an asset (best-effort).</summary>
    Task DeleteAssetFilesAsync(string? originalRelPath, string? thumbRelPath, CancellationToken ct = default);
}
