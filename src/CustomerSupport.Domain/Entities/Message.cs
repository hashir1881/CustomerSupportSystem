using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Domain.Entities;

/// <summary>
/// Individual message sent in a support request conversation.
/// </summary>
public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid SenderUserId { get; set; }
    public User SenderUser { get; set; } = null!;

    public MessageType Type { get; set; } = MessageType.Text;

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DeliveredAt { get; set; }

    public DateTimeOffset? ReadAt { get; set; }
}
