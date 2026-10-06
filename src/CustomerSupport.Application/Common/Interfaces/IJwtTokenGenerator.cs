using CustomerSupport.Application.Dtos;

namespace CustomerSupport.Application.Common.Interfaces;

/// <summary>
/// Contract for generating cryptographically signed JWT access tokens.
/// </summary>
public interface IJwtTokenGenerator
{
    (string Token, DateTimeOffset ExpiresAt) GenerateToken(UserDto user);
}
