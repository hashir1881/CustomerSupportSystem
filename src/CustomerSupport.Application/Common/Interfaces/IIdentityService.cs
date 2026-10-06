using CustomerSupport.Application.Dtos;

namespace CustomerSupport.Application.Common.Interfaces;

/// <summary>
/// Service for validating domain identity, auto-provisioning new corporate users,
/// and retrieving server-enforced user roles from PostgreSQL.
/// </summary>
public interface IIdentityService
{
    Task<UserDto> AuthenticateDomainUserAsync(string domainUsername, CancellationToken cancellationToken = default);
    Task<UserDto> AuthenticateCredentialsAsync(string domainUsername, string password, CancellationToken cancellationToken = default);
}
