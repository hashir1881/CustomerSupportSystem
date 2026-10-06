namespace CustomerSupport.Domain.Entities;

/// <summary>
/// Immutable audit log entry recording critical actions across the system.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The user who performed the action (null if system-triggered).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Name of the action (e.g. ComplaintCreated, ComplaintClaimed, StatusChanged).
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// The entity type affected (e.g. SupportRequest, User).
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the target entity.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// JSON-serialized payload recording details of the change (PostgreSQL JSONB compatible).
    /// </summary>
    public string? MetadataJson { get; set; }
}
