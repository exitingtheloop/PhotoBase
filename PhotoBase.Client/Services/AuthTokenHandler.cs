using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace PhotoBase.Client.Services;

/// <summary>
/// HTTP message handler that auto-attaches the JWT Bearer token
/// from localStorage to every outgoing request.
/// </summary>
public class AuthTokenHandler : DelegatingHandler
{
    private readonly IJSRuntime _js;

    public AuthTokenHandler(IJSRuntime js) => _js = js;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", "photobase_token");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch (InvalidOperationException)
        {
            // JS interop not available (prerender) — send without token
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
