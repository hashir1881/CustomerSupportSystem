using CustomerSupport.Application.Common.Interfaces;
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Api.Services;

/// <summary>
/// Development identity provider for Phase 3.
/// Reads identity from the 'X-User-Username' header or defaults to 'CORP\customer.ahmed'.
/// The server determines the user's role from PostgreSQL—the client cannot self-assign roles.
/// Replaced by JWT ClaimsPrincipal in Phase 5.
/// </summary>
public class HeaderCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HeaderCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? DomainUsername
    {
        get
        {
            var header = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Username"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(header))
            {
                return header.Trim();
            }

            // Default development identity
            return @"CORP\customer.ahmed";
        }
    }

    public Guid? UserId => null;

    public UserRole? Role => null;

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(DomainUsername);
}
