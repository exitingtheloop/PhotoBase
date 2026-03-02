namespace PhotoBase.Shared.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Login request payload.
/// </summary>
public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
