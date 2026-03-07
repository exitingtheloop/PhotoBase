namespace PhotoBase.API.Configuration;

/// <summary>
/// JWT authentication settings, bound from appsettings "Jwt" section.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 signing key. Must be ? 32 characters in production.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Token issuer (iss claim).</summary>
    public string Issuer { get; set; } = "PhotoBase";

    /// <summary>Token audience (aud claim).</summary>
    public string Audience { get; set; } = "PhotoBase";

    /// <summary>Token lifetime in minutes.</summary>
    public int ExpiresInMinutes { get; set; } = 480; // 8 hours
}
