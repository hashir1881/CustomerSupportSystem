using CustomerSupport.Application.Dtos;

namespace CustomerSupport_CsDesktop.Services;

/// <summary>
/// Thread-safe in-memory store for active Customer Service Agent authentication credentials.
/// </summary>
public interface IAuthenticationState
{
    AuthResponseDto? CurrentAuth { get; }
    UserDto? CurrentUser { get; }
    string? AccessToken { get; }
    bool IsAuthenticated { get; }
    bool IsAuthorizedAgent { get; }
    DateTimeOffset? ExpiresAt { get; }
    bool IsExpired { get; }
    string ActiveDomainUsername { get; set; }

    event Action? AuthenticationChanged;

    void SetAuth(AuthResponseDto auth);
    void Clear();
}

public class AuthenticationState : IAuthenticationState
{
    private readonly object _lock = new();
    private AuthResponseDto? _currentAuth;
    private string _activeDomainUsername = @"TIH\agent.sara"; // Default to CS Agent 1 for dev

    public AuthResponseDto? CurrentAuth
    {
        get { lock (_lock) { return _currentAuth; } }
    }

    public UserDto? CurrentUser => CurrentAuth?.User;

    public string? AccessToken => CurrentAuth?.Token;

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

    public bool IsAuthorizedAgent => CurrentUser != null &&
        (CurrentUser.Role == CustomerSupport.Domain.Enums.UserRole.CustomerService ||
         CurrentUser.Role == CustomerSupport.Domain.Enums.UserRole.Administrator);

    public DateTimeOffset? ExpiresAt => CurrentAuth?.ExpiresAt;

    public bool IsExpired => !ExpiresAt.HasValue || DateTimeOffset.UtcNow >= ExpiresAt.Value.AddSeconds(-30);

    public string ActiveDomainUsername
    {
        get { lock (_lock) { return _activeDomainUsername; } }
        set
        {
            lock (_lock) { _activeDomainUsername = value; }
            AuthenticationChanged?.Invoke();
        }
    }

    public event Action? AuthenticationChanged;

    public void SetAuth(AuthResponseDto auth)
    {
        lock (_lock)
        {
            _currentAuth = auth;
            if (auth.User != null)
            {
                _activeDomainUsername = auth.User.DomainUsername;
            }
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
