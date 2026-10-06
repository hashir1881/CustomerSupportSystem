namespace CustomerSupport.Application.Dtos;

/// <summary>
/// Encapsulates the application JWT access token, expiration time, and authenticated user profile.
/// </summary>
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTimeOffset ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}
