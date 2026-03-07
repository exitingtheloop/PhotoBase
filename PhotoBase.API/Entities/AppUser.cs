using System.ComponentModel.DataAnnotations;

namespace PhotoBase.API.Entities;

/// <summary>
/// Simple user entity for JWT auth. No ASP.NET Identity — just email + hashed password + role.
/// </summary>
public class AppUser
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    /// <summary>BCrypt hash of the password.</summary>
    [Required, MaxLength(200)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Role: "Admin" or "Internal".</summary>
    [Required, MaxLength(50)]
    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
