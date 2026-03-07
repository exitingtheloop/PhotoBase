namespace PhotoBase.API.Services;

/// <summary>
/// Computes a content hash for duplicate detection (ADR-0005).
/// </summary>
public interface IHashService
{
    /// <summary>Compute SHA-256 hash of the given stream. Stream position is reset to 0 after hashing.</summary>
    Task<string> ComputeSha256Async(Stream stream, CancellationToken ct = default);
}
