namespace PhotoBase.Shared.Responses;

/// <summary>
/// Returned after successful login.
/// </summary>
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
 public string Role { get; set; } = string.Empty;
public DateTime ExpiresUtc { get; set; }
}
