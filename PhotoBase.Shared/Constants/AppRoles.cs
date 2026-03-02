namespace PhotoBase.Shared.Constants;

/// <summary>
/// Central place for role and claim name strings — no magic strings in controllers/client.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Internal = "Internal";

    /// <summary>Policy/role string that allows both Admin and Internal.</summary>
    public const string Authenticated = "Internal,Admin";
}

public static class AppClaims
{
    public const string Role = "role";
    public const string Email = "email";
    public const string UserId = "sub";
}
