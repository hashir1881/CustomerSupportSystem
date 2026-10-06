using CustomerSupport.Application.Dtos;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace CustomerSupport_CsDesktop.Services;

public interface IChatHubService
{
    bool IsConnected { get; }
    event Action<ChatMessageDto>? MessageReceived;
    event Action<ChatMessageDto>? TicketNotificationReceived;
    event Action<Guid, string, bool>? UserTyping;
    event Action<Guid, IReadOnlyList<ChatMessageDto>>? HistoryLoaded;

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task JoinTicketChatAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task LeaveTicketChatAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<ChatMessageDto?> SendMessageAsync(Guid ticketId, string content, int messageType = 1, string? attachmentUrl = null, CancellationToken cancellationToken = default);
    Task SendTypingAsync(Guid ticketId, bool isTyping, CancellationToken cancellationToken = default);
}

public class ChatHubService : IChatHubService, IAsyncDisposable
{
    private readonly IAuthenticationState _authenticationState;
    private readonly ILogger<ChatHubService> _logger;
    private HubConnection? _hubConnection;

    public event Action<ChatMessageDto>? MessageReceived;
    public event Action<ChatMessageDto>? TicketNotificationReceived;
    public event Action<Guid, string, bool>? UserTyping;
    public event Action<Guid, IReadOnlyList<ChatMessageDto>>? HistoryLoaded;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public ChatHubService(IAuthenticationState authenticationState, ILogger<ChatHubService> logger)
    {
        _authenticationState = authenticationState;
        _logger = logger;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;

        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }

        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5239/hubs/support-chat", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(_authenticationState.AccessToken);
            })
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
            .Build();

        _hubConnection.On<ChatMessageDto>("ReceiveMessage", message =>
        {
            MessageReceived?.Invoke(message);
        });

        _hubConnection.On<ChatMessageDto>("TicketMessageNotification", message =>
        {
            TicketNotificationReceived?.Invoke(message);
        });

        _hubConnection.On<Guid, string, bool>("UserTyping", (ticketId, user, isTyping) =>
        {
            UserTyping?.Invoke(ticketId, user, isTyping);
        });

        _hubConnection.On<Guid, IReadOnlyList<ChatMessageDto>>("LoadHistory", (ticketId, history) =>
        {
            HistoryLoaded?.Invoke(ticketId, history);
        });

        try
        {
            await _hubConnection.StartAsync(cancellationToken);
            _logger.LogInformation("SignalR connected successfully to support-chat hub.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to connect to SignalR hub: {Message}", ex.Message);
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync(cancellationToken);
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async Task JoinTicketChatAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            await ConnectAsync(cancellationToken);
        }

        if (_hubConnection != null && IsConnected)
        {
            await _hubConnection.InvokeAsync("JoinTicketChat", ticketId, cancellationToken);
        }
    }

    public async Task LeaveTicketChatAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        if (_hubConnection != null && IsConnected)
        {
            await _hubConnection.InvokeAsync("LeaveTicketChat", ticketId, cancellationToken);
        }
    }

    public async Task<ChatMessageDto?> SendMessageAsync(Guid ticketId, string content, int messageType = 1, string? attachmentUrl = null, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            await ConnectAsync(cancellationToken);
        }

        if (_hubConnection != null && IsConnected)
        {
            return await _hubConnection.InvokeAsync<ChatMessageDto>("SendMessage", ticketId, content, messageType, attachmentUrl, cancellationToken);
        }

        return null;
    }

    public async Task SendTypingAsync(Guid ticketId, bool isTyping, CancellationToken cancellationToken = default)
    {
        if (_hubConnection != null && IsConnected)
        {
            await _hubConnection.InvokeAsync("SendTyping", ticketId, isTyping, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
