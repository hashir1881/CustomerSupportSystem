using CustomerSupport.Application.Dtos;

namespace CustomerSupport_CustomerDesktop.Services;

/// <summary>
/// Thread-safe in-memory store for active authentication credentials and user profile.
/// </summary>
public interface IAuthenticationState
{
    AuthResponseDto? CurrentAuth { get; }
    UserDto? CurrentUser { get; }
    string? AccessToken { get; }
    bool IsAuthenticated { get; }

    event Action? AuthenticationChanged;

    void SetAuth(AuthResponseDto auth);
    void Clear();
}

public class AuthenticationState : IAuthenticationState
{
    private readonly object _lock = new();
    private AuthResponseDto? _currentAuth;

    public AuthResponseDto? CurrentAuth
    {
        get { lock (_lock) { return _currentAuth; } }
    }

    public UserDto? CurrentUser => CurrentAuth?.User;

    public string? AccessToken => CurrentAuth?.Token;

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

    public event Action? AuthenticationChanged;

    public void SetAuth(AuthResponseDto auth)
    {
        lock (_lock)
        {
            _currentAuth = auth;
        }
        AuthenticationChanged?.Invoke();
    }

    public void Clear()
    {
        lock (_lock)
        {
            _currentAuth = null;
        }
        AuthenticationChanged?.Invoke();
    }
}
