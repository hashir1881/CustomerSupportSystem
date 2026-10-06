using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using CustomerSupport.Application.Common.Interfaces;
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Api.Services;

/// <summary>
/// Resolves the current user identity from validated JWT claims in HttpContext.User.
/// Supports backward-compatibility for local development and test suite overrides.
/// </summary>
public class ClaimsCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idClaim = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                       ?? Principal?.FindFirst("sub")?.Value;

            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public string? DomainUsername
    {
        get
        {
            // 1. From authenticated JWT Claims
            var claimName = Principal?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
                         ?? Principal?.FindFirst("unique_name")?.Value
                         ?? Principal?.FindFirst(ClaimTypes.Upn)?.Value;

            if (!string.IsNullOrWhiteSpace(claimName))
            {
                return claimName;
            }

            // 2. Windows Identity name if authenticated via Negotiate
            if (OperatingSystem.IsWindows() && Principal?.Identity is WindowsIdentity windowsIdentity && !string.IsNullOrWhiteSpace(windowsIdentity.Name))
            {
                return windowsIdentity.Name;
            }

            if (Principal?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(Principal.Identity.Name))
            {
                return Principal.Identity.Name;
            }

            // 3. Fallback to development/test header if present
            var headerOverride = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Username"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerOverride))
            {
                return headerOverride.Trim();
            }

            return null;
        }
    }

    public UserRole? Role
    {
        get
        {
            var roleClaim = Principal?.FindFirst(ClaimTypes.Role)?.Value
                         ?? Principal?.FindFirst("role")?.Value;

            return Enum.TryParse<UserRole>(roleClaim, true, out var role) ? role : null;
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            if (Principal?.Identity?.IsAuthenticated == true)
            {
                return true;
            }

            var headerOverride = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Username"].FirstOrDefault();
            return !string.IsNullOrWhiteSpace(headerOverride);
        }
    }
}
