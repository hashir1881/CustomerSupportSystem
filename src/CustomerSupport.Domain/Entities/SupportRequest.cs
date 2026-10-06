using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Domain.Entities;

/// <summary>
/// The central entity representing a customer support request / complaint.
/// </summary>
public class SupportRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the customer who submitted the request.
    /// </summary>
    public Guid RequesterUserId { get; set; }
    public User RequesterUser { get; set; } = null!;

    /// <summary>
    /// Foreign key to the CS agent who claimed the request (null while Pending).
    /// </summary>
    public Guid? AssignedAgentUserId { get; set; }
    public User? AssignedAgentUser { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public RequestStatus Status { get; set; } = RequestStatus.Created;

    public RequestPriority Priority { get; set; } = RequestPriority.Medium;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? ClaimedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    /// <summary>
    /// Optimistic concurrency token mapped to PostgreSQL's system 'xmin' column.
    /// Protects against simultaneous ticket claiming race conditions.
    /// </summary>
    public uint RowVersion { get; set; }

    // Navigation property (1-to-1 relationship with Conversation)
    public Conversation? Conversation { get; set; }
}
