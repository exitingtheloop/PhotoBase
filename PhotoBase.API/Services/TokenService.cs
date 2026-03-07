using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PhotoBase.API.Configuration;
using PhotoBase.API.Entities;
using PhotoBase.Shared.Constants;

namespace PhotoBase.API.Services;

/// <summary>
/// Generates JWT tokens for authenticated users.
/// </summary>
public class TokenService
{
    private readonly JwtOptions _opts;

    public TokenService(IOptions<JwtOptions> opts) => _opts = opts.Value;

    public (string Token, DateTime ExpiresUtc) GenerateToken(AppUser user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_opts.ExpiresInMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
    {
    new(AppClaims.UserId, user.Id.ToString()),
   new(AppClaims.Email, user.Email),
    new(AppClaims.Role, user.Role),
        // Also add standard ClaimTypes.Role so [Authorize(Roles="...")] works
       new(ClaimTypes.Role, user.Role),
        };

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
   audience: _opts.Audience,
            claims: claims,
 expires: expires,
   signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
