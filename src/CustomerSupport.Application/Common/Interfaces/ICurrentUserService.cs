using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Common.Interfaces;

/// <summary>
/// Abstraction for the currently authenticated user.
/// During Phase 3 development, this is resolved via request headers or dev defaults.
/// In Phase 5, this seamlessly resolves from validated JWT claims in HttpContext.User.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? DomainUsername { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
}
