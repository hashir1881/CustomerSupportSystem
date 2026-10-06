using CustomerSupport.Application.Common.Interfaces;
using CustomerSupport.Application.Dtos;
using CustomerSupport.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CustomerSupport.Api.Hubs;

/// <summary>
/// Real-time SignalR Hub for bidirectional live support conversations and instant status broadcasting.
/// </summary>
[Authorize]
public class SupportChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly ILogger<SupportChatHub> _logger;

    public SupportChatHub(IMessageService messageService, ILogger<SupportChatHub> logger)
    {
        _messageService = messageService;
        _logger = logger;
    }

    /// <summary>
    /// Joins the caller to the specific ticket group and delivers existing chat history.
    /// </summary>
    public async Task JoinTicketChat(Guid ticketId)
    {
        var groupName = GetGroupName(ticketId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation("Connection {ConnectionId} joined ticket group {GroupName}", Context.ConnectionId, groupName);

        var history = await _messageService.GetMessagesByTicketIdAsync(ticketId);
        await Clients.Caller.SendAsync("LoadHistory", ticketId, history);
    }

    /// <summary>
    /// Leaves the ticket chat group.
    /// </summary>
    public async Task LeaveTicketChat(Guid ticketId)
    {
        var groupName = GetGroupName(ticketId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} left ticket group {GroupName}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Sends a text or screenshot image message in real-time, persisting to PostgreSQL first.
    /// </summary>
    public async Task<ChatMessageDto> SendMessage(Guid ticketId, string content, int messageType = 1, string? attachmentUrl = null)
    {
        var type = (MessageType)messageType;
        var messageDto = await _messageService.SendMessageAsync(ticketId, content, type, attachmentUrl);

        var groupName = GetGroupName(ticketId);
        await Clients.Group(groupName).SendAsync("ReceiveMessage", messageDto);
        await Clients.All.SendAsync("TicketMessageNotification", messageDto);

        return messageDto;
    }

    /// <summary>
    /// Broadcasts typing indicators to others in the ticket group.
    /// </summary>
    public async Task SendTyping(Guid ticketId, bool isTyping)
    {
        var groupName = GetGroupName(ticketId);
        var callerName = Context.User?.Identity?.Name ?? "Participant";
        await Clients.OthersInGroup(groupName).SendAsync("UserTyping", ticketId, callerName, isTyping);
    }

    private static string GetGroupName(Guid ticketId) => $"ticket-{ticketId}";
}
