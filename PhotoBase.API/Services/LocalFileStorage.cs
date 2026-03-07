using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PhotoBase.API.Configuration;

namespace PhotoBase.API.Services;

public partial class LocalFileStorage : IFileStorage
{
    private readonly string _originalsRoot;
    private readonly string _thumbsRoot;

    public LocalFileStorage(IOptions<StorageOptions> opts)
    {
        var o = opts.Value;
        var basePath = Path.GetFullPath(o.BasePath);
        _originalsRoot = Path.Combine(basePath, o.OriginalsFolder);
        _thumbsRoot = Path.Combine(basePath, o.ThumbsFolder);

        Directory.CreateDirectory(_originalsRoot);
        Directory.CreateDirectory(_thumbsRoot);
    }

    public async Task<string> SaveOriginalAsync(Guid assetId, string safeFileName, Stream content, CancellationToken ct = default)
    {
        var dir = Path.Combine(_originalsRoot, assetId.ToString("N"));
        Directory.CreateDirectory(dir);

        var sanitized = SanitizeFileName(safeFileName);
        var fullPath = Path.Combine(dir, sanitized);

        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(fs, ct);

        // Return path relative to storage base (originals/{id}/{file})
        return Path.Combine("originals", assetId.ToString("N"), sanitized).Replace('\\', '/');
    }

    public async Task<string> SaveThumbnailAsync(Guid assetId, Stream content, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_thumbsRoot);

        var thumbFile = $"{assetId:N}.jpg";
        var fullPath = Path.Combine(_thumbsRoot, thumbFile);

        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(fs, ct);

        return $"thumbs/{thumbFile}";
    }

    public string GetFullPath(string relativePath)
    {
        var basePath = Path.GetDirectoryName(_originalsRoot)!; // parent of originals = basePath
        return Path.GetFullPath(Path.Combine(basePath, relativePath));
    }

    public Task DeleteAssetFilesAsync(string? originalRelPath, string? thumbRelPath, CancellationToken ct = default)
    {
        TryDelete(originalRelPath);
        TryDelete(thumbRelPath);
        return Task.CompletedTask;
    }

    private void TryDelete(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;

        try
        {
            var full = GetFullPath(relativePath);
            if (File.Exists(full))
                File.Delete(full);
        }
        catch
        {
            // Best-effort; log in production
        }
    }

    /// <summary>
    /// Strip anything dangerous from a file name: keep only alphanum, dash, underscore, dot.
    /// </summary>
    private static string SanitizeFileName(string raw)
    {
        // Take only the file name portion (no directory traversal)
        var name = Path.GetFileName(raw);

        if (string.IsNullOrWhiteSpace(name))
            name = "upload";

        // Replace unsafe chars
        name = UnsafeCharsRegex().Replace(name, "_");

        // Collapse multiple underscores
        name = MultiUnderscoreRegex().Replace(name, "_");

        // Ensure it's not too long
        if (name.Length > 200)
        {
            var ext = Path.GetExtension(name);
            name = name[..(200 - ext.Length)] + ext;
        }

        return name;
    }

    [GeneratedRegex(@"[^a-zA-Z0-9._\-]")]
    private static partial Regex UnsafeCharsRegex();

    [GeneratedRegex(@"_{2,}")]
    private static partial Regex MultiUnderscoreRegex();
}
