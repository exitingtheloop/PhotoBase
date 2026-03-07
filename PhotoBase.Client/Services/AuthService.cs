using System.Net.Http.Json;
using Microsoft.JSInterop;
using PhotoBase.Shared.Requests;
using PhotoBase.Shared.Responses;

namespace PhotoBase.Client.Services;

/// <summary>
/// Handles login/logout, stores JWT in localStorage, and notifies the
/// <see cref="JwtAuthStateProvider"/> when auth state changes.
/// </summary>
public class AuthService
{
    private const string TokenKey = "photobase_token";
    private const string EmailKey = "photobase_email";
    private const string RoleKey = "photobase_role";

    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly JwtAuthStateProvider _authState;

    public AuthService(HttpClient http, IJSRuntime js, JwtAuthStateProvider authState)
    {
        _http = http;
        _js = js;
        _authState = authState;
    }

    /// <summary>
    /// Attempt to log in. Returns the LoginResponse on success, null on failure.
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login",
         new LoginRequest { Email = email, Password = password });

        if (!response.IsSuccessStatusCode)
            return null;

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (login is null || string.IsNullOrEmpty(login.Token))
            return null;

        // Persist to localStorage
        await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, login.Token);
        await _js.InvokeVoidAsync("localStorage.setItem", EmailKey, login.Email);
        await _js.InvokeVoidAsync("localStorage.setItem", RoleKey, login.Role);

        // Notify Blazor auth system
        _authState.NotifyAuthChanged();

        return login;
    }

    /// <summary>
    /// Log out — clear stored token and notify auth state.
    /// </summary>
    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", EmailKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", RoleKey);

        _authState.NotifyAuthChanged();
    }

    /// <summary>Read the stored JWT token, or null if not logged in.</summary>
    public async Task<string?> GetTokenAsync()
        => await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);

    /// <summary>Read the stored email.</summary>
    public async Task<string?> GetEmailAsync()
    => await _js.InvokeAsync<string?>("localStorage.getItem", EmailKey);

    /// <summary>Read the stored role.</summary>
    public async Task<string?> GetRoleAsync()
     => await _js.InvokeAsync<string?>("localStorage.getItem", RoleKey);
}
