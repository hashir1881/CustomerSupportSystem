using System.Net.Http.Headers;

namespace CustomerSupport_CsDesktop.Services;

/// <summary>
/// HTTP delegating handler that attaches the Bearer JWT token to all outgoing requests.
/// Also forwards the active CS agent domain username header for dev identity negotiation.
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

        // Forward active agent identity
        if (!string.IsNullOrWhiteSpace(_authenticationState.ActiveDomainUsername))
        {
            request.Headers.Remove("X-User-Username");
            request.Headers.Add("X-User-Username", _authenticationState.ActiveDomainUsername);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
