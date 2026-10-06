using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CustomerSupport.Application.Dtos;
using CustomerSupport.Application.Services;
using CustomerSupport.Domain.Enums;
using CustomerSupport_CustomerDesktop.Models;
using CustomerSupport_CustomerDesktop.Services;

namespace CustomerSupport_CustomerDesktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAuthApiClient _authApiClient;
    private readonly ISupportRequestApiClient _requestApiClient;
    private readonly IAuthenticationState _authenticationState;
    private readonly ISupportTriageEngine _triageEngine;
    private readonly IChatHubService _chatHubService;
    private readonly IAttachmentApiClient _attachmentApiClient;

    [ObservableProperty]
    private string? _currentUserDisplayName;

    [ObservableProperty]
    private string? _currentUserDomain;

    [ObservableProperty]
    private string? _currentUserDepartment;

    [ObservableProperty]
    private string? _currentUserRole;

    [ObservableProperty]
    private ObservableCollection<SupportRequestDto> _requests = new();

    [ObservableProperty]
    private SupportRequestDto? _selectedRequest;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _errorMessage;

    // Chatbot state
    [ObservableProperty]
    private ObservableCollection<ChatMessageModel> _chatMessages = new();

    [ObservableProperty]
    private string _chatInputText = string.Empty;

    [ObservableProperty]
    private bool _isChatMode = true;

    [ObservableProperty]
    private bool _isSidebarOpen = true;

    [RelayCommand]
    public void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
    }

    [ObservableProperty]
    private bool _isBotProcessing;

    // Chat History replay state
    [ObservableProperty]
    private bool _isChatHistoryMode;

    [ObservableProperty]
    private ObservableCollection<ChatMessageModel> _historyChatMessages = new();

    [ObservableProperty]
    private string _historyChatTitle = string.Empty;

    [ObservableProperty]
    private string _historyChatSubtitle = string.Empty;

    [ObservableProperty]
    private bool _hasHistoryTranscript;

    // Live Chat between Customer and Assigned Agent
    [ObservableProperty]
    private ObservableCollection<ChatMessageModel> _liveChatMessages = new();

    [ObservableProperty]
    private string _liveChatInputText = string.Empty;

    [ObservableProperty]
    private bool _isLiveChatActive;

    [ObservableProperty]
    private bool _isAgentTyping;

    [ObservableProperty]
    private string? _agentTypingNotification;

    [ObservableProperty]
    private bool _isUploadingScreenshot;

    // Active Directory Corporate Sign In State
    [ObservableProperty]
    private bool _isLoginViewVisible;

    [ObservableProperty]
    private string _loginUsername = @"TIH\user.john";

    [ObservableProperty]
    private string _loginPassword = "Password123!";

    [ObservableProperty]
    private string? _loginErrorMessage;

    [ObservableProperty]
    private bool _isLoggingIn;

    public bool IsTicketDetailsMode => !IsChatMode && !IsChatHistoryMode;

    partial void OnIsChatModeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsTicketDetailsMode));
    }

    partial void OnIsChatHistoryModeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsTicketDetailsMode));
    }

    public MainViewModel(
        IAuthApiClient authApiClient,
        ISupportRequestApiClient requestApiClient,
        IAuthenticationState authenticationState,
        ISupportTriageEngine triageEngine,
        IChatHubService chatHubService,
        IAttachmentApiClient attachmentApiClient)
    {
        _authApiClient = authApiClient;
        _requestApiClient = requestApiClient;
        _authenticationState = authenticationState;
        _triageEngine = triageEngine;
        _chatHubService = chatHubService;
        _attachmentApiClient = attachmentApiClient;

        _authenticationState.AuthenticationChanged += OnAuthenticationChanged;

        _chatHubService.MessageReceived += OnLiveMessageReceived;
        _chatHubService.TicketNotificationReceived += OnTicketNotificationReceived;
        _chatHubService.HistoryLoaded += OnHistoryLoaded;
        _chatHubService.UserTyping += OnUserTyping;
    }

    private void OnAuthenticationChanged()
    {
        var user = _authenticationState.CurrentUser;
        if (user != null)
        {
            CurrentUserDisplayName = user.DisplayName;
            CurrentUserDomain = user.DomainUsername;
            CurrentUserDepartment = user.Department;
            CurrentUserRole = user.Role.ToString();
        }
    }

    private void OnTicketNotificationReceived(ChatMessageDto message)
    {
        if (message.SenderUserId == _authenticationState.CurrentUser?.Id) return;

        App.MainWindowInstance?.DispatcherQueue?.TryEnqueue(() =>
        {
            // If the user is currently viewing this ticket in live chat, don't increment unread count
            if (SelectedRequest != null && SelectedRequest.Id == message.TicketId && IsLiveChatActive)
            {
                return;
            }

            var ticket = Requests.FirstOrDefault(r => r.Id == message.TicketId);
            if (ticket != null)
            {
                ticket.UnreadCount++;
            }
        });
    }

    private void OnLiveMessageReceived(ChatMessageDto message)
    {
        if (SelectedRequest == null || message.TicketId != SelectedRequest.Id) return;

        App.MainWindowInstance?.DispatcherQueue?.TryEnqueue(() =>
        {
            AddOrUpdateLiveMessage(message, isMine: message.SenderUserId == _authenticationState.CurrentUser?.Id);
        });
    }

    private void OnHistoryLoaded(Guid ticketId, IReadOnlyList<ChatMessageDto> history)
    {
        if (SelectedRequest == null || ticketId != SelectedRequest.Id) return;

        App.MainWindowInstance?.DispatcherQueue?.TryEnqueue(() =>
        {
            foreach (var msg in history)
            {
                AddOrUpdateLiveMessage(msg, isMine: msg.SenderUserId == _authenticationState.CurrentUser?.Id);
            }
        });
    }

    private void OnUserTyping(Guid ticketId, string username, bool isTyping)
    {
        if (SelectedRequest == null || ticketId != SelectedRequest.Id) return;

        App.MainWindowInstance?.DispatcherQueue?.TryEnqueue(() =>
        {
            IsAgentTyping = isTyping;
            AgentTypingNotification = isTyping ? $"{username} is typing..." : null;
        });
    }

    private void AddOrUpdateLiveMessage(ChatMessageDto message, bool isMine)
    {
        if (ChatMessages.Any(m => m.Id == message.Id)) return;

        var fullImageUrl = string.IsNullOrWhiteSpace(message.AttachmentUrl) ? null
            : (message.AttachmentUrl.StartsWith("http") ? message.AttachmentUrl : $"http://localhost:5239{message.AttachmentUrl}");

        ChatMessages.Add(new ChatMessageModel
        {
            Id = message.Id,
            Sender = isMine ? ChatSender.User : ChatSender.Agent,
            Content = message.Content,
            ImageUrl = fullImageUrl,
            IsMine = isMine,
            SenderDisplayName = message.SenderDisplayName,
            Timestamp = message.SentAt
        });

        if (!isMine)
        {
            IsLiveChatActive = true;
        }
    }

    [RelayCommand]
    public async Task InitializeAsync()

    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            StatusMessage = "Connecting to Windows Identity Service...";

            var auth = await _authApiClient.LoginWithWindowsAsync();
            StatusMessage = $"Authenticated as {auth.User.DisplayName}";

            await _chatHubService.ConnectAsync();
            await LoadRequestsAsync();
            StartNewChat();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Authentication failed: {ex.Message}";
            StatusMessage = "Error connecting to server";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshRequestsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            StatusMessage = "Refreshing complaints...";

            await LoadRequestsAsync();
            StatusMessage = "Complaints updated";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to refresh complaints: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadRequestsAsync()
    {
        var list = await _requestApiClient.GetMyRequestsAsync();
        Requests.Clear();
        foreach (var item in list)
        {
            Requests.Add(item);
        }
    }

    [RelayCommand]
    public async Task SendChatMessageAsync()
    {
        var text = ChatInputText?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        // If in ticket mode (SelectedRequest != null)
        if (SelectedRequest != null)
        {
            if (SelectedRequest.Status == RequestStatus.Closed)
            {
                ErrorMessage = "This ticket has been closed and cannot receive new messages.";
                return;
            }

            ChatInputText = string.Empty;
            _ = NotifyTypingAsync(false);

            try
            {
                await _chatHubService.SendMessageAsync(SelectedRequest.Id, text, 1, null);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to send message: {ex.Message}";
            }
            return;
        }

        ChatInputText = string.Empty;

        // 1. Add user bubble
        ChatMessages.Add(new ChatMessageModel
        {
            Sender = ChatSender.User,
            Content = text,
            IsMine = true
        });

        IsBotProcessing = true;
        ErrorMessage = null;

        try
        {
            // Give a tiny natural typing feel
            await Task.Delay(150);

            // 2. Process through triage engine
            var result = _triageEngine.ProcessUserInput(text);

            // 3. Add bot reply
            ChatMessages.Add(new ChatMessageModel
            {
                Sender = ChatSender.Assistant,
                Content = result.BotReply,
                QuickReplies = result.QuickReplies,
                IsMine = false
            });

            // 4. If triage completed, auto-submit the ticket
            if (result.IsReadyToSubmitTicket && result.TicketDto != null)
            {
                StatusMessage = "Registering ticket in backend database...";
                var created = await _requestApiClient.CreateRequestAsync(result.TicketDto);

                Requests.Insert(0, created);
                SelectedRequest = created;

                // Add Confirmation Ticket Card
                ChatMessages.Add(new ChatMessageModel
                {
                    Sender = ChatSender.Assistant,
                    Content = $"Your complaint has been logged and assigned to the Customer Service pool.\n\nTicket ID: {created.Id}\nStatus: {created.StatusName}\nPriority: {created.PriorityName}",
                    IsTicketCard = true,
                    CreatedTicketId = created.Id,
                    CreatedTicketSubject = created.Subject,
                    CreatedTicketStatus = created.StatusName,
                    QuickReplies = new[] { "Start New Support Chat" }
                });

                // Add System Event Divider
                ChatMessages.Add(new ChatMessageModel
                {
                    Sender = ChatSender.System,
                    Content = $"⏳ Ticket #{created.Id.ToString().Substring(0, 8)} logged in pool. Waiting for an available support agent to claim and join live chat...",
                    IsSystemEvent = true
                });

                StatusMessage = $"Ticket #{created.Id.ToString().Substring(0, 8)} created successfully";

                try
                {
                    await _chatHubService.JoinTicketChatAsync(created.Id);
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error during triage: {ex.Message}";
            ChatMessages.Add(new ChatMessageModel
            {
                Sender = ChatSender.Assistant,
                Content = $"⚠️ An error occurred while creating your ticket: {ex.Message}. Please try again.",
                QuickReplies = new[] { "Retry" },
                IsMine = false
            });
        }
        finally
        {
            IsBotProcessing = false;
        }
    }

    [RelayCommand]
    public async Task SelectQuickReplyAsync(string? reply)
    {
        if (string.IsNullOrWhiteSpace(reply)) return;

        if (reply == "Start New Support Chat")
        {
            StartNewChat();
            return;
        }

        if (reply == "View Ticket Details")
        {
            if (Requests.Count > 0)
            {
                SelectTicket(Requests[0]);
            }
            return;
        }

        ChatInputText = reply;
        await SendChatMessageAsync();
    }

    [RelayCommand]
    public void StartNewChat()
    {
        SelectedRequest = null;
        IsLiveChatActive = false;
        IsAgentTyping = false;
        AgentTypingNotification = null;
        ChatMessages.Clear();

        var greeting = _triageEngine.GetInitialGreeting();
        ChatMessages.Add(new ChatMessageModel
        {
            Sender = ChatSender.Assistant,
            Content = greeting.BotReply,
            QuickReplies = greeting.QuickReplies,
            IsMine = false
        });
        IsChatMode = true;
        IsChatHistoryMode = false;
    }

    [RelayCommand]
    public void SelectTicket(SupportRequestDto? ticket)
    {
        if (ticket == null) return;
        ticket.UnreadCount = 0;
        SelectedRequest = ticket;
        HasHistoryTranscript = ticket.HasTranscript;
        IsChatMode = true;
        IsChatHistoryMode = false;

        _ = LoadTicketChatAsync(ticket);
    }

    private async Task LoadTicketChatAsync(SupportRequestDto? request)
    {
        ChatMessages.Clear();
        IsAgentTyping = false;
        AgentTypingNotification = null;

        if (request == null)
        {
            IsLiveChatActive = false;
            return;
        }

        // 1. Populate the intake dialogue from the triage transcript (where the user requested the complaint)
        var parsed = TriageTranscriptParser.Parse(request.Description);
        foreach (var (sender, content) in parsed.ChatMessages)
        {
            var isUser = sender.Equals("Customer", StringComparison.OrdinalIgnoreCase);
            var (cleanText, imageUrl) = ExtractAttachmentUrl(content);

            ChatMessages.Add(new ChatMessageModel
            {
                Sender = isUser ? ChatSender.User : ChatSender.Assistant,
                Content = cleanText,
                ImageUrl = imageUrl,
                IsMine = isUser,
                Timestamp = request.CreatedAt
            });
        }

        // 2. Add Confirmation Ticket Card
        ChatMessages.Add(new ChatMessageModel
        {
            Sender = ChatSender.Assistant,
            Content = $"Support Ticket #{request.Id.ToString().Substring(0, 8)}:\nDepartment: {request.DepartmentTitle}\nStatus: {request.StatusName}\nPriority: {request.PriorityName}",
            IsTicketCard = true,
            CreatedTicketId = request.Id,
            CreatedTicketSubject = request.DepartmentTitle,
            CreatedTicketStatus = request.StatusName
        });

        // 3. Add System status divider
        IsLiveChatActive = request.Status == RequestStatus.Claimed || request.Status == RequestStatus.InProgress;

        if (request.Status == RequestStatus.Created || request.Status == RequestStatus.Pending)
        {
            ChatMessages.Add(new ChatMessageModel
            {
                Sender = ChatSender.System,
                Content = "⏳ Ticket awaiting support agent assignment from pool. Once claimed, live agent chat will be active.",
                IsSystemEvent = true
            });
        }
        else if (IsLiveChatActive)
        {
            ChatMessages.Add(new ChatMessageModel
            {
                Sender = ChatSender.System,
                Content = $"🟢 Agent {request.AssignedAgent?.DisplayName ?? "Support Agent"} has claimed this ticket and joined the live chat.",
                IsSystemEvent = true
            });
        }
        else
        {
            ChatMessages.Add(new ChatMessageModel
            {
                Sender = ChatSender.System,
                Content = $"🔒 Ticket marked as {request.StatusName} on {request.ClosedAtFormatted ?? request.ResolvedAtFormatted ?? "recently"}.",
                IsSystemEvent = true
            });
        }

        // 4. Connect to SignalR hub to receive real-time messages and load existing live chat history
        try
        {
            await _chatHubService.JoinTicketChatAsync(request.Id);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to join live chat: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task SendLiveMessageAsync()
    {
        if (SelectedRequest == null || string.IsNullOrWhiteSpace(LiveChatInputText)) return;
        var text = LiveChatInputText.Trim();
        LiveChatInputText = string.Empty;

        try
        {
            await _chatHubService.SendMessageAsync(SelectedRequest.Id, text, 1, null);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to send live message: {ex.Message}";
        }
    }

    public async Task SendLiveScreenshotAsync(string base64Data, string fileName = "screenshot.png")
    {
        if (SelectedRequest == null || string.IsNullOrWhiteSpace(base64Data)) return;

        try
        {
            IsUploadingScreenshot = true;
            StatusMessage = "Uploading screenshot attachment...";

            var uploadUrl = await _attachmentApiClient.UploadScreenshotBase64Async(base64Data, fileName);
            if (!string.IsNullOrWhiteSpace(uploadUrl))
            {
                await _chatHubService.SendMessageAsync(SelectedRequest.Id, "Screenshot attached", 4, uploadUrl);
                StatusMessage = "Screenshot sent successfully";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to upload and send screenshot: {ex.Message}";
        }
        finally
        {
            IsUploadingScreenshot = false;
        }
    }

    public async Task AttachTriageScreenshotAsync(string base64Data, string fileName = "screenshot.png")
    {
        if (string.IsNullOrWhiteSpace(base64Data)) return;

        try
        {
            IsUploadingScreenshot = true;
            StatusMessage = "Uploading screenshot attachment...";

            var uploadUrl = await _attachmentApiClient.UploadScreenshotBase64Async(base64Data, fileName);
            if (!string.IsNullOrWhiteSpace(uploadUrl))
            {
                var fullImageUrl = uploadUrl.StartsWith("http") ? uploadUrl : $"http://localhost:5239{uploadUrl}";

                // 1. Add user bubble with thumbnail
                ChatMessages.Add(new ChatMessageModel
                {
                    Sender = ChatSender.User,
                    Content = $"Attached error screenshot ({fileName})",
                    ImageUrl = fullImageUrl,
                    IsMine = true
                });

                // 2. Feed into triage engine
                IsBotProcessing = true;
                await Task.Delay(150);

                var triageInput = $"[Screenshot: {uploadUrl}] User attached error screenshot: {fileName}";
                var result = _triageEngine.ProcessUserInput(triageInput);

                ChatMessages.Add(new ChatMessageModel
                {
                    Sender = ChatSender.Assistant,
                    Content = result.BotReply,
                    QuickReplies = result.QuickReplies
                });

                if (result.IsReadyToSubmitTicket && result.TicketDto != null)
                {
                    StatusMessage = "Registering ticket in backend database...";
                    var created = await _requestApiClient.CreateRequestAsync(result.TicketDto);
                    Requests.Insert(0, created);
                    SelectedRequest = created;

                    ChatMessages.Add(new ChatMessageModel
                    {
                        Sender = ChatSender.Assistant,
                        Content = $"Your complaint has been logged and assigned to the Customer Service pool.\n\nTicket ID: {created.Id}\nStatus: {created.StatusName}\nPriority: {created.PriorityName}",
                        IsTicketCard = true,
                        CreatedTicketId = created.Id,
                        CreatedTicketSubject = created.Subject,
                        CreatedTicketStatus = created.StatusName,
                        QuickReplies = new[] { "Start New Support Chat" }
                    });

                    // Add System Event Divider
                    ChatMessages.Add(new ChatMessageModel
                    {
                        Sender = ChatSender.System,
                        Content = $"⏳ Ticket #{created.Id.ToString().Substring(0, 8)} logged in pool. Waiting for an available support agent to claim and join live chat...",
                        IsSystemEvent = true
                    });

                    StatusMessage = $"Ticket #{created.Id.ToString().Substring(0, 8)} created successfully";

                    try
                    {
                        await _chatHubService.JoinTicketChatAsync(created.Id);
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to attach screenshot: {ex.Message}";
        }
        finally
        {
            IsUploadingScreenshot = false;
            IsBotProcessing = false;
        }
    }

    public async Task AttachScreenshotAsync(string base64Data, string fileName = "screenshot.png")
    {
        if (SelectedRequest != null)
        {
            await SendLiveScreenshotAsync(base64Data, fileName);
        }
        else
        {
            await AttachTriageScreenshotAsync(base64Data, fileName);
        }
    }

    public async Task NotifyTypingAsync(bool isTyping)
    {
        if (SelectedRequest == null || !IsLiveChatActive) return;
        try
        {
            await _chatHubService.SendTypingAsync(SelectedRequest.Id, isTyping);
        }
        catch { }
    }

    [RelayCommand]
    public async Task SignOutAsync()
    {
        try
        {
            await _chatHubService.DisconnectAsync();
        }
        catch { }

        _authenticationState.Clear();
        SelectedRequest = null;
        Requests.Clear();
        ChatMessages.Clear();
        LiveChatMessages.Clear();
        IsLiveChatActive = false;
        IsChatHistoryMode = false;
        IsChatMode = false;
        LoginErrorMessage = null;
        IsLoginViewVisible = true;
        StatusMessage = "Signed out. Please sign in with your corporate Active Directory account.";
    }

    [RelayCommand]
    public async Task SignInAsync()
    {
        if (string.IsNullOrWhiteSpace(LoginUsername) || string.IsNullOrWhiteSpace(LoginPassword))
        {
            LoginErrorMessage = "Please enter both Domain Username and Password.";
            return;
        }

        try
        {
            IsLoggingIn = true;
            LoginErrorMessage = null;

            var auth = await _authApiClient.LoginWithCredentialsAsync(LoginUsername.Trim(), LoginPassword);
            if (auth?.User == null)
            {
                LoginErrorMessage = "Invalid credentials. Please verify your Active Directory username and password.";
                return;
            }

            IsLoginViewVisible = false;
            LoginPassword = string.Empty;
            CurrentUserDisplayName = auth.User.DisplayName;
            CurrentUserDomain = auth.User.DomainUsername;
            CurrentUserDepartment = auth.User.Department;
            CurrentUserRole = auth.User.RoleName;

            StatusMessage = $"Signed in as {auth.User.DisplayName}";
            await _chatHubService.ConnectAsync();
            await LoadRequestsAsync();
            StartNewChat();
        }
        catch (Exception ex)
        {
            LoginErrorMessage = $"Sign In failed: {ex.Message}";
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

    [RelayCommand]
    public void CancelLogin()
    {
        if (_authenticationState.IsAuthenticated)
        {
            IsLoginViewVisible = false;
            LoginErrorMessage = null;
        }
    }

    [RelayCommand]
    public void ViewChatHistory()
    {
        if (SelectedRequest == null) return;

        HistoryChatMessages.Clear();
        var parsed = TriageTranscriptParser.Parse(SelectedRequest.Description);

        foreach (var (sender, content) in parsed.ChatMessages)
        {
            var isUser = sender.Equals("Customer", StringComparison.OrdinalIgnoreCase);
            var (cleanText, imageUrl) = ExtractAttachmentUrl(content);

            HistoryChatMessages.Add(new ChatMessageModel
            {
                Sender = isUser ? ChatSender.User : ChatSender.Assistant,
                Content = cleanText,
                ImageUrl = imageUrl,
                IsMine = isUser,
                Timestamp = SelectedRequest.CreatedAt
            });
        }

        HistoryChatTitle = $"Chat History — {SelectedRequest.DepartmentTitle}";
        HistoryChatSubtitle = $"Recorded during triage on {SelectedRequest.CreatedAtFormatted}";
        IsChatHistoryMode = true;
        IsChatMode = false;
    }

    [RelayCommand]
    public async Task OpenAttachmentAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            var uri = new Uri(url);
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to open attachment: {ex.Message}";
        }
    }

    [RelayCommand]
    public void CloseChatHistory()
    {
        IsChatHistoryMode = false;
    }

    [RelayCommand]
    public void ReturnToChat()
    {
        IsChatMode = true;
        IsChatHistoryMode = false;
    }

    [RelayCommand]
    public async Task CloseComplaintAsync()
    {
        if (SelectedRequest == null) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var closed = await _requestApiClient.CloseRequestAsync(SelectedRequest.Id);

            var index = Requests.IndexOf(SelectedRequest);
            if (index >= 0)
            {
                Requests[index] = closed;
                SelectedRequest = closed;
            }

            StatusMessage = "Complaint closed";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to close complaint: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static (string CleanText, string? ImageUrl) ExtractAttachmentUrl(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return (content, null);

        var startIdx = content.IndexOf("[Screenshot:", StringComparison.OrdinalIgnoreCase);
        if (startIdx < 0) startIdx = content.IndexOf("[Image:", StringComparison.OrdinalIgnoreCase);

        if (startIdx >= 0)
        {
            var endIdx = content.IndexOf(']', startIdx);
            if (endIdx > startIdx)
            {
                var colonIdx = content.IndexOf(':', startIdx);
                var rawUrl = content.Substring(colonIdx + 1, endIdx - (colonIdx + 1)).Trim();
                var fullUrl = rawUrl.StartsWith("http") ? rawUrl : $"http://localhost:5239{rawUrl}";
                var clean = content.Remove(startIdx, endIdx - startIdx + 1).Trim();
                return (clean, fullUrl);
            }
        }

        return (content, null);
    }
}

