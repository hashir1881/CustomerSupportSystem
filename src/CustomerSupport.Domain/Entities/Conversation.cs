namespace CustomerSupport.Domain.Entities;

/// <summary>
/// Container for bidirectional chat messages linked to a SupportRequest.
/// </summary>
public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the parent SupportRequest (1-to-1 relationship).
    /// </summary>
    public Guid SupportRequestId { get; set; }
    public SupportRequest SupportRequest { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ClosedAt { get; set; }

    // Navigation property
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
