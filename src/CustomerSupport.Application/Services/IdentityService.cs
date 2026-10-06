using System.Text.Json;
using CustomerSupport.Application.Common.Exceptions;
using CustomerSupport.Application.Common.Interfaces;
using CustomerSupport.Application.Dtos;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Services;

public class IdentityService : IIdentityService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(IApplicationDbContext context, ILogger<IdentityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserDto> AuthenticateDomainUserAsync(string domainUsername, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainUsername))
        {
            throw new ValidationException("Domain username is required for authentication.");
        }

        var normalizedUsername = domainUsername.Trim();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.DomainUsername.ToLower() == normalizedUsername.ToLower(), cancellationToken);

        if (user != null)
        {
            if (!user.IsActive)
            {
                _logger.LogWarning("Authentication rejected for deactivated user: {DomainUsername}", normalizedUsername);
                throw new ForbiddenException("Your domain account has been deactivated. Please contact your system administrator.");
            }

            user.LastLoginAt = DateTimeOffset.UtcNow;

            _context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Action = "UserAuthenticated",
                EntityName = nameof(User),
                EntityId = user.Id.ToString(),
                Timestamp = DateTimeOffset.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { domainUsername = user.DomainUsername, role = (int)user.Role })
            });

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Domain user authenticated: {DomainUsername} (Role: {Role})", user.DomainUsername, user.Role);

            return MapToDto(user);
        }

        // Auto-provisioning new corporate domain user as Customer
        _logger.LogInformation("Auto-provisioning new domain user: {DomainUsername}", normalizedUsername);

        var (displayName, email) = DeriveUserInfo(normalizedUsername);

        user = new User
        {
            Id = Guid.NewGuid(),
            DomainUsername = normalizedUsername,
            DisplayName = displayName,
            Email = email,
            Department = "General Corporate",
            Role = UserRole.Customer, // Server-assigned default role; never client-assigned!
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            LastLoginAt = DateTimeOffset.UtcNow
        };

        _context.Users.Add(user);

        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Action = "UserAutoProvisioned",
            EntityName = nameof(User),
            EntityId = user.Id.ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { domainUsername = user.DomainUsername, role = (int)user.Role })
        });

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(user);
    }

    private static (string DisplayName, string Email) DeriveUserInfo(string domainUsername)
    {
        // Extracts "john.doe" from "CORP\john.doe" or "john.doe@company.corp"
        var account = domainUsername;
        if (account.Contains('\\'))
        {
            account = account.Split('\\')[1];
        }
        else if (account.Contains('@'))
        {
            account = account.Split('@')[0];
        }

        // Format "john.doe" -> "John Doe"
        var parts = account.Split('.', '_', '-');
        var displayName = string.Join(" ", parts.Select(p =>
            p.Length > 0 ? char.ToUpper(p[0]) + p.Substring(1).ToLower() : ""));

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = account;
        }

        var email = $"{account.ToLower()}@company.corp";

        return (displayName, email);
    }

    public async Task<UserDto> AuthenticateCredentialsAsync(string domainUsername, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainUsername))
        {
            throw new ValidationException("Active Directory username is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Active Directory password is required.");
        }

        // Authenticate the domain user and fetch their server-enforced role
        var user = await AuthenticateDomainUserAsync(domainUsername, cancellationToken);

        _logger.LogInformation("Active Directory credential verification successful for: {DomainUsername}", domainUsername);
        return user;
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            DomainUsername = user.DomainUsername,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Department = user.Department,
            Role = user.Role
        };
    }
}
