using CustomerSupport.Application.Dtos;
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Common.Interfaces;

public interface IMessageService
{
    Task<IReadOnlyList<ChatMessageDto>> GetMessagesByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<ChatMessageDto> SendMessageAsync(Guid ticketId, string content, MessageType messageType = MessageType.Text, string? attachmentUrl = null, CancellationToken cancellationToken = default);
}
