using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoBase.API.Data;
using PhotoBase.API.Services;
using PhotoBase.Shared.Requests;
using PhotoBase.Shared.Responses;

namespace PhotoBase.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly PhotoBaseDbContext _db;
    private readonly TokenService _tokens;

    public AuthController(PhotoBaseDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    /// <summary>
    /// Authenticate with email + password and receive a JWT.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
    [FromBody] LoginRequest request,
     CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem();

        var user = await _db.Users
           .AsNoTracking()
       .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Problem(
              title: "Invalid credentials",
                 detail: "Email or password is incorrect.",
           statusCode: StatusCodes.Status401Unauthorized);
        }

        var (token, expires) = _tokens.GenerateToken(user);

        return Ok(new LoginResponse
        {
            Token = token,
            Email = user.Email,
            Role = user.Role,
            ExpiresUtc = expires
        });
    }
}
