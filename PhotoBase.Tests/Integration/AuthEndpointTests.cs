using System.Net;
using System.Net.Http.Json;
using PhotoBase.Shared.Responses;

namespace PhotoBase.Tests.Integration;

/// <summary>
/// Integration tests for POST /api/auth/login.
/// </summary>
public class AuthEndpointTests : IClassFixture<PhotoBaseTestFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(PhotoBaseTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_AdminCredentials_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@photobase.dev", password = "Admin123!" });

        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(login);
        Assert.False(string.IsNullOrEmpty(login.Token));
        Assert.Equal("admin@photobase.dev", login.Email);
        Assert.Equal("Admin", login.Role);
        Assert.True(login.ExpiresUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_InternalCredentials_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "user@photobase.dev", password = "User123!" });

        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(login);
        Assert.False(string.IsNullOrEmpty(login.Token));
        Assert.Equal("user@photobase.dev", login.Email);
        Assert.Equal("Internal", login.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
      new { email = "admin@photobase.dev", password = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
  new { email = "nobody@photobase.dev", password = "Password123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_MissingEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
              new { email = "", password = "Password123!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
