namespace CustomerSupport.Application.Dtos;

/// <summary>
/// Credentials payload for Active Directory corporate domain login.
/// </summary>
public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
