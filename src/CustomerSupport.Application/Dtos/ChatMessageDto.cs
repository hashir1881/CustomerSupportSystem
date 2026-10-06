using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Dtos;

/// <summary>
/// DTO representing a chat message in a support conversation, supporting text and image attachments.
/// </summary>
public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid TicketId { get; set; }
    public Guid SenderUserId { get; set; }
    public string SenderDisplayName { get; set; } = string.Empty;
    public string SenderDomainUsername { get; set; } = string.Empty;
    public UserRole SenderRole { get; set; }
    public string SenderRoleName => SenderRole.ToString();
    public MessageType MessageType { get; set; } = MessageType.Text;
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public DateTimeOffset SentAt { get; set; }
    public string SentAtFormatted => SentAt.ToLocalTime().ToString("d-MMM-yyyy h:mm tt");
    public bool IsFromAgent => SenderRole == UserRole.CustomerService || SenderRole == UserRole.Administrator;
}
