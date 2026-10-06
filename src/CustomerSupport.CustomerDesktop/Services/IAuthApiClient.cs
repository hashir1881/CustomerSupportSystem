using System.Net.Http.Json;
using System.Text.Json;
using CustomerSupport.Application.Dtos;

namespace CustomerSupport_CustomerDesktop.Services;

public interface IAuthApiClient
{
    Task<AuthResponseDto> LoginWithWindowsAsync(CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginWithCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);
}

public class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationState _authenticationState;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthApiClient(HttpClient httpClient, IAuthenticationState authenticationState)
    {
        _httpClient = httpClient;
        _authenticationState = authenticationState;
    }

    public async Task<AuthResponseDto> LoginWithWindowsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync("api/auth/windows-login", null, cancellationToken);
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize authentication response from server.");

        _authenticationState.SetAuth(auth);
        return auth;
    }

    public async Task<AuthResponseDto> LoginWithCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            Username = username.Trim(),
            Password = password
        };

        var response = await _httpClient.PostAsJsonAsync("api/auth/login", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize authentication response from server.");

        _authenticationState.SetAuth(auth);
        return auth;
    }
}
