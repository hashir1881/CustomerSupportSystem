using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Domain.Entities;

/// <summary>
/// Represents an authenticated enterprise user mapped from Active Directory / Windows Identity.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The Windows / Active Directory logon name (e.g. CORP\john.doe). Unique in the system.
    /// </summary>
    public string DomainUsername { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Customer;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<SupportRequest> SubmittedRequests { get; set; } = new List<SupportRequest>();
    public ICollection<SupportRequest> AssignedRequests { get; set; } = new List<SupportRequest>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
}
