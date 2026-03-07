using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PhotoBase.Tests;

/// <summary>
/// Helpers for generating test JWT tokens with specific characteristics
/// (expired, wrong key, specific roles) for auth regression tests.
/// </summary>
internal static class TokenHelper
{
    /// <summary>
    /// Generate a valid-looking JWT signed with the given key and role.
    /// </summary>
    public static string GenerateToken(string secretKey, string role,
        int expiresInMinutes = 60, string email = "test@photobase.dev")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
{
            new Claim("sub", Guid.NewGuid().ToString()),
   new Claim("email", email),
            new Claim("role", role),
    new Claim(ClaimTypes.Role, role),
      };

        var token = new JwtSecurityToken(
                  issuer: "PhotoBase",
      audience: "PhotoBase",
          claims: claims,
         expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
       signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generate a JWT that expired 1 hour ago (signed with the correct key).
    /// </summary>
    public static string GenerateExpiredToken(string secretKey, string role = "Admin")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
         new Claim("sub", Guid.NewGuid().ToString()),
new Claim("email", "expired@photobase.dev"),
          new Claim("role", role),
         new Claim(ClaimTypes.Role, role),
    };

        var token = new JwtSecurityToken(
            issuer: "PhotoBase",
           audience: "PhotoBase",
    claims: claims,
        notBefore: DateTime.UtcNow.AddHours(-2),
                expires: DateTime.UtcNow.AddHours(-1), // expired 1 hour ago
             signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
