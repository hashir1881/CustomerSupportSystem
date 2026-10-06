using System.Net.Http.Headers;

namespace CustomerSupport_CustomerDesktop.Services;

/// <summary>
/// HTTP delegating handler that attaches the Bearer JWT token to all outgoing requests.
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IAuthenticationState _authenticationState;

    public AuthHeaderHandler(IAuthenticationState authenticationState)
    {
        _authenticationState = authenticationState;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_authenticationState.IsAuthenticated)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authenticationState.AccessToken);
        }

        // Attach local Windows identity header for dev/intranet fallback if not set
        if (!request.Headers.Contains("X-User-Username"))
        {
            request.Headers.Add("X-User-Username", $@"{Environment.UserDomainName}\{Environment.UserName}");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
