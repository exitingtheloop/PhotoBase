using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace PhotoBase.Client.Services;

/// <summary>
/// Custom <see cref="AuthenticationStateProvider"/> that reads a JWT from
/// localStorage and exposes its claims to the Blazor auth system.
/// Parses the JWT payload via base64 decode — no server-side JWT library needed in WASM.
/// </summary>
public class JwtAuthStateProvider : AuthenticationStateProvider
{
    private const string TokenKey = "photobase_token";
    private readonly IJSRuntime _js;

    public JwtAuthStateProvider(IJSRuntime js) => _js = js;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        string? token;
        try
        {
            token = await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        }
        catch (InvalidOperationException)
        {
            // JS interop not available during prerender — return anonymous
            return Anonymous();
        }

        if (string.IsNullOrWhiteSpace(token))
            return Anonymous();

        var claims = ParseClaimsFromJwt(token);
        if (claims is null)
            return Anonymous();

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
    }

    /// <summary>
    /// Called by <see cref="AuthService"/> after login/logout to re-evaluate auth state.
    /// </summary>
    public void NotifyAuthChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static AuthenticationState Anonymous()
  => new(new ClaimsPrincipal(new ClaimsIdentity()));

    /// <summary>
    /// Decode the JWT payload (middle segment) and extract claims.
    /// Returns null if the token is malformed or expired.
    /// </summary>
    private static List<Claim>? ParseClaimsFromJwt(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;

            var payload = parts[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);
            if (keyValues is null) return null;

            // Check expiration
            if (keyValues.TryGetValue("exp", out var expElem))
            {
                var exp = DateTimeOffset.FromUnixTimeSeconds(expElem.GetInt64());
                if (exp < DateTimeOffset.UtcNow)
                    return null;
            }

            var claims = new List<Claim>();

            foreach (var (key, value) in keyValues)
            {
                if (value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in value.EnumerateArray())
                        claims.Add(new Claim(key, item.ToString()));
                }
                else
                {
                    claims.Add(new Claim(key, value.ToString()));
                }
            }

            // Ensure ClaimTypes.Role is populated from "role" claims
            var roleClaims = claims.Where(c => c.Type == "role").ToList();
            foreach (var rc in roleClaims)
            {
                if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == rc.Value))
                    claims.Add(new Claim(ClaimTypes.Role, rc.Value));
            }

            return claims;
        }
        catch
        {
            return null;
        }
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        // JWT base64url: replace URL-safe chars and add padding
        base64 = base64.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
