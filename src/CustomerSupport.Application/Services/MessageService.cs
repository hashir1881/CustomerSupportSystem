using CustomerSupport.Application.Common.Exceptions;
using CustomerSupport.Application.Common.Interfaces;
using CustomerSupport.Application.Dtos;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Services;

public class MessageService : IMessageService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MessageService> _logger;

    public MessageService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<MessageService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetMessagesByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Messages)
                .ThenInclude(m => m.SenderUser)
            .FirstOrDefaultAsync(c => c.SupportRequestId == ticketId, cancellationToken);

        if (conversation == null)
        {
            return Array.Empty<ChatMessageDto>();
        }

        return conversation.Messages
            .OrderBy(m => m.SentAt)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                TicketId = ticketId,
                SenderUserId = m.SenderUserId,
                SenderDisplayName = m.SenderUser?.DisplayName ?? "User",
                SenderDomainUsername = m.SenderUser?.DomainUsername ?? string.Empty,
                SenderRole = m.SenderUser?.Role ?? UserRole.Customer,
                MessageType = m.Type,
                Content = m.Content,
                AttachmentUrl = m.Type == MessageType.Image ? m.Content : null,
                SentAt = m.SentAt
            })
            .ToList();
    }

    public async Task<ChatMessageDto> SendMessageAsync(
        Guid ticketId,
        string content,
        MessageType messageType = MessageType.Text,
        string? attachmentUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(attachmentUrl))
        {
            throw new ValidationException("Message content or attachment is required.");
        }

        var request = await _context.SupportRequests
            .Include(r => r.Conversation)
            .FirstOrDefaultAsync(r => r.Id == ticketId, cancellationToken);

        if (request == null)
        {
            throw new NotFoundException(nameof(SupportRequest), ticketId);
        }

        // Determine sender user
        User? senderUser = null;
        if (_currentUserService.UserId.HasValue)
        {
            senderUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId.Value, cancellationToken);
        }

        if (senderUser == null && !string.IsNullOrWhiteSpace(_currentUserService.DomainUsername))
        {
            senderUser = await _context.Users.FirstOrDefaultAsync(u => u.DomainUsername.ToLower() == _currentUserService.DomainUsername.ToLower(), cancellationToken);
        }

        if (senderUser == null)
        {
            throw new ForbiddenException("User not authenticated to send messages.");
        }

        // Ensure conversation exists
        if (request.Conversation == null)
        {
            var newConversation = new Conversation
            {
                Id = Guid.NewGuid(),
                SupportRequestId = ticketId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _context.Conversations.Add(newConversation);
            request.Conversation = newConversation;
        }

        var effectiveContent = messageType == MessageType.Image && !string.IsNullOrWhiteSpace(attachmentUrl)
            ? attachmentUrl
            : content.Trim();

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = request.Conversation.Id,
            SenderUserId = senderUser.Id,
            SenderUser = senderUser,
            Type = messageType,
            Content = effectiveContent,
            SentAt = DateTimeOffset.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved chat message {MessageId} for Ticket #{TicketId} from {User}", message.Id, ticketId, senderUser.DomainUsername);

        return new ChatMessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            TicketId = ticketId,
            SenderUserId = senderUser.Id,
            SenderDisplayName = senderUser.DisplayName,
            SenderDomainUsername = senderUser.DomainUsername,
            SenderRole = senderUser.Role,
            MessageType = message.Type,
            Content = message.Content,
            AttachmentUrl = message.Type == MessageType.Image ? message.Content : null,
            SentAt = message.SentAt
        };
    }
}
