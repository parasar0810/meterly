using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace Meterly.Client.Auth;

public class JwtAuthenticationHeaderHandler : DelegatingHandler
{
    private readonly IJSInProcessRuntime _jsRuntime;

    public JwtAuthenticationHeaderHandler(IJSInProcessRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get the token from local storage synchronously (WASM supports this)
        var token = _jsRuntime.Invoke<string>("localStorage.getItem", "jwt_token");

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
