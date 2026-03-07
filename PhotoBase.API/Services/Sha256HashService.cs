using System.Security.Cryptography;

namespace PhotoBase.API.Services;

public class Sha256HashService : IHashService
{
    public async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Ensure we hash from the beginning
        if (stream.CanSeek)
            stream.Position = 0;

        var hashBytes = await SHA256.HashDataAsync(stream, ct);

        // Reset so callers can read the stream again (e.g. to save the file)
        if (stream.CanSeek)
            stream.Position = 0;

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
