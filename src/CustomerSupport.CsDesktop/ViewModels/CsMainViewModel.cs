using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CustomerSupport.Application.Dtos;
using CustomerSupport.Application.Services;
using CustomerSupport.Domain.Enums;
using CustomerSupport_CsDesktop.Models;
using CustomerSupport_CsDesktop.Services;

namespace CustomerSupport_CsDesktop.ViewModels;

public partial class CsMainViewModel : ObservableObject
{
    private readonly IAuthApiClient _authApiClient;
    private readonly ICsSupportRequestApiClient _requestApiClient;
    private readonly IAuthenticationState _authenticationState;
    private readonly IChatHubService _chatHubService;
    private readonly IAttachmentApiClient _attachmentApiClient;

    // Agent Profile
    [ObservableProperty]
    private string? _currentAgentDisplayName;

    [ObservableProperty]
    private string? _currentAgentDomain;

    [ObservableProperty]
    private string? _currentAgentDepartment;

    [ObservableProperty]
    private string? _currentAgentRole;

    [ObservableProperty]
    private bool _isAuthorizedAgent;

    // Dev Agent Switcher
    public IReadOnlyList<string> AvailableDevAgents { get; } = new[]
    {
        @"TIH\agent.sara",
        @"TIH\agent.ali",
        @"TIH\admin.user"
    };

    [ObservableProperty]
    private string _selectedDevAgent = @"TIH\agent.sara";

    // Queues
    [ObservableProperty]
    private ObservableCollection<SupportRequestDto> _unassignedRequests = new();

    [ObservableProperty]
    private ObservableCollection<SupportRequestDto> _myAssignedRequests = new();

    [ObservableProperty]
    private ObservableCollection<SupportRequestDto> _allRequests = new();

    [ObservableProperty]
    private ObservableCollection<SupportRequestDto> _displayedRequests = new();

    [ObservableProperty]
    private int _selectedQueueIndex = 0; // 0: Unassigned, 1: My Active, 2: All

    [ObservableProperty]
    private bool _isUnassignedTabActive = true;

    [ObservableProperty]
    private bool _isMyActiveTabActive;

    [ObservableProperty]
    private bool _isAllTicketsTabActive;

    [ObservableProperty]
    private int _unassignedCount;

    [ObservableProperty]
    private int _myAssignedCount;

    [ObservableProperty]
    private int _allCount;

    // Selected Ticket & Actions
    [ObservableProperty]
    private SupportRequestDto? _selectedRequest;

    [ObservableProperty]
    private bool _canClaim;

    [ObservableProperty]
    private bool _canResolve;

    [ObservableProperty]
    private bool _canClose;

    [ObservableProperty]
    private bool _isTicketClosedOrResolved;

    // Chat History Replay State
    [ObservableProperty]
    private bool _isViewingChatHistory;

    [ObservableProperty]
    private ObservableCollection<ChatMessageModel> _historyChatMessages = new();

    [ObservableProperty]
    private string _historyChatTitle = string.Empty;

    [ObservableProperty]
    private string _historyChatSubtitle = string.Empty;

    // Status / Feedback
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _conflictMessage;

    // Live Chat between CS Agent and Customer
    [ObservableProperty]
    private ObservableCollection<ChatMessageModel> _liveChatMessages = new();

    [ObservableProperty]
    private string _liveChatInputText = string.Empty;

    [ObservableProperty]
    private bool _isLiveChatActive;

    [ObservableProperty]
    private bool _isCustomerTyping;

    [ObservableProperty]
    private string? _customerTypingNotification;

    [ObservableProperty]
    private bool _isUploadingScreenshot;

    [ObservableProperty]
    private bool _isSidebarOpen = true;

    [RelayCommand]
    public void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
    }

    // Active Directory Corporate Sign In State
    [ObservableProperty]
    private bool _isLoginViewVisible;

    [ObservableProperty]
    private string _loginUsername = @"TIH\agent.sara";

    [ObservableProperty]
    private string _loginPassword = "Password123!";

    [ObservableProperty]
    private string? _loginErrorMessage;

    [ObservableProperty]
    private bool _isLoggingIn;

    public CsMainViewModel(
        IAuthApiClient authApiClient,
        ICsSupportRequestApiClient requestApiClient,
        IAuthenticationState authenticationState,
        IChatHubService chatHubService,
        IAttachmentApiClient attachmentApiClient)
    {
        _authApiClient = authApiClient;
        _requestApiClient = requestApiClient;
        _authenticationState = authenticationState;
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
            CurrentAgentDisplayName = user.DisplayName;
            CurrentAgentDomain = user.DomainUsername;
            CurrentAgentDepartment = user.Department;
            CurrentAgentRole = user.Role.ToString();
            IsAuthorizedAgent = _authenticationState.IsAuthorizedAgent;
        }
    }

    partial void OnSelectedQueueIndexChanged(int value)
    {
        IsUnassignedTabActive = value == 0;
        IsMyActiveTabActive = value == 1;
        IsAllTicketsTabActive = value == 2;
        UpdateDisplayedQueue();
    }

    partial void OnSelectedRequestChanged(SupportRequestDto? value)
    {
        if (value != null)
        {
            value.UnreadCount = 0; // WhatsApp style: reset count on viewing
        }
        UpdateActionButtons();
        _ = LoadTicketChatAsync(value);
    }

    private void OnTicketNotificationReceived(ChatMessageDto message)
    {
        if (message.SenderUserId == _authenticationState.CurrentUser?.Id) return;

        App.MainWindowInstance?.DispatcherQueue?.TryEnqueue(() =>
        {
            // If the agent is currently viewing this ticket in live chat, don't increment unread count
            if (SelectedRequest != null && SelectedRequest.Id == message.TicketId && IsLiveChatActive)
            {
                return;
            }

            var ticket = AllRequests.FirstOrDefault(r => r.Id == message.TicketId)
                         ?? DisplayedRequests.FirstOrDefault(r => r.Id == message.TicketId);
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
            IsCustomerTyping = isTyping;
            CustomerTypingNotification = isTyping ? $"{username} is typing..." : null;
        });
    }

    private void AddOrUpdateLiveMessage(ChatMessageDto message, bool isMine)
    {
        if (LiveChatMessages.Any(m => m.Id == message.Id)) return;

        var fullImageUrl = string.IsNullOrWhiteSpace(message.AttachmentUrl) ? null
            : (message.AttachmentUrl.StartsWith("http") ? message.AttachmentUrl : $"http://localhost:5239{message.AttachmentUrl}");

        LiveChatMessages.Add(new ChatMessageModel
        {
            Id = message.Id,
            Sender = isMine ? ChatSender.Agent : ChatSender.Customer,
            Content = message.Content,
            ImageUrl = fullImageUrl,
            IsMine = isMine,
            SenderDisplayName = message.SenderDisplayName,
            Timestamp = message.SentAt
        });
    }

    private async Task LoadTicketChatAsync(SupportRequestDto? request)
    {
        LiveChatMessages.Clear();
        IsCustomerTyping = false;
        CustomerTypingNotification = null;

        if (request == null)
        {
            IsLiveChatActive = false;
            return;
        }

        // 1. Populate the intake dialogue from the triage transcript (where the user requested the complaint)
        var parsed = TriageTranscriptParser.Parse(request.Description);
        foreach (var (sender, content) in parsed.ChatMessages)
        {
            var isCustomer = sender.Equals("Customer", StringComparison.OrdinalIgnoreCase);
            var (cleanText, imageUrl) = ExtractAttachmentUrl(content);

            LiveChatMessages.Add(new ChatMessageModel
            {
                Sender = isCustomer ? ChatSender.Customer : ChatSender.Assistant,
                Content = cleanText,
                ImageUrl = imageUrl,
                IsMine = false,
                Timestamp = request.CreatedAt
            });
        }

        // 2. Add System status divider
        IsLiveChatActive = request.Status == RequestStatus.Claimed || request.Status == RequestStatus.InProgress;

        if (request.Status == RequestStatus.Created || request.Status == RequestStatus.Pending)
        {
            LiveChatMessages.Add(new ChatMessageModel
            {
                Sender = ChatSender.System,
                Content = "📥 Ticket unassigned in pool. Claim this ticket to start live chat with customer.",
                IsSystemEvent = true
            });
        }
        else if (IsLiveChatActive)
        {
            LiveChatMessages.Add(new ChatMessageModel
            {
                Sender = ChatSender.System,
                Content = $"🟢 Ticket claimed by {request.AssignedAgent?.DisplayName ?? "Agent"} on {request.ClaimedAtFormatted ?? "recently"}.",
                IsSystemEvent = true
            });
        }
        else
        {
            LiveChatMessages.Add(new ChatMessageModel
            {
                Sender = ChatSender.System,
                Content = $"🔒 Ticket marked as {request.StatusName} on {request.ClosedAtFormatted ?? request.ResolvedAtFormatted ?? "recently"}.",
                IsSystemEvent = true
            });
        }

        // 3. Connect to SignalR hub to receive real-time messages and load existing live chat history
        try
        {
            await _chatHubService.JoinTicketChatAsync(request.Id);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to join ticket live chat: {ex.Message}";
        }
    }

    private void UpdateActionButtons()
    {
        if (SelectedRequest == null)
        {
            CanClaim = false;
            CanResolve = false;
            CanClose = false;
            IsTicketClosedOrResolved = false;
            return;
        }

        var currentUserId = _authenticationState.CurrentUser?.Id;
        var isAdmin = _authenticationState.CurrentUser?.Role == UserRole.Administrator;

        // Can only claim if ticket is Created or Pending and unassigned
        CanClaim = (SelectedRequest.Status == RequestStatus.Created || SelectedRequest.Status == RequestStatus.Pending) &&
                   SelectedRequest.AssignedAgent == null;

        // Can resolve if ticket is Claimed or InProgress and assigned to me (or I am Admin)
        CanResolve = (SelectedRequest.Status == RequestStatus.Claimed || SelectedRequest.Status == RequestStatus.InProgress) &&
                     (SelectedRequest.AssignedAgent?.Id == currentUserId || isAdmin);

        // Can close if ticket is not already closed
        CanClose = SelectedRequest.Status != RequestStatus.Closed;

        // Concluded / Closed / Resolved status check for read-only banner
        IsTicketClosedOrResolved = SelectedRequest.Status == RequestStatus.Closed || SelectedRequest.Status == RequestStatus.Resolved;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            ConflictMessage = null;
            StatusMessage = "Connecting to Identity Service...";

            _authenticationState.ActiveDomainUsername = SelectedDevAgent;
            var auth = await _authApiClient.LoginWithWindowsAsync(SelectedDevAgent);

            if (!IsAuthorizedAgent)
            {
                ErrorMessage = $"Access Restricted: User '{auth.User.DomainUsername}' with role '{auth.User.RoleName}' is not authorized to access the Customer Service Agent Console.";
                StatusMessage = "Access Denied";
                return;
            }

            StatusMessage = $"Authenticated as {auth.User.DisplayName} ({auth.User.RoleName})";
            await _chatHubService.ConnectAsync();
            await RefreshQueuesAsync();
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
    public async Task SwitchAgentAsync(string? agentDomain)
    {
        if (string.IsNullOrWhiteSpace(agentDomain)) return;

        SelectedDevAgent = agentDomain;
        SelectedRequest = null;
        IsViewingChatHistory = false;
        await InitializeAsync();
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (_authenticationState.IsExpired || !_authenticationState.IsAuthenticated)
        {
            _authenticationState.ActiveDomainUsername = SelectedDevAgent;
            var auth = await _authApiClient.LoginWithWindowsAsync(SelectedDevAgent);
            if (auth.User != null)
            {
                CurrentAgentDisplayName = auth.User.DisplayName;
                CurrentAgentDepartment = auth.User.Department;
                CurrentAgentRole = auth.User.RoleName;
                IsAuthorizedAgent = _authenticationState.IsAuthorizedAgent;
            }
        }
    }

    [RelayCommand]
    public async Task RefreshQueuesAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            ConflictMessage = null;
            StatusMessage = "Refreshing ticket queues...";

            await EnsureAuthenticatedAsync();

            IReadOnlyList<SupportRequestDto> all;
            try
            {
                all = await _requestApiClient.GetAllRequestsAsync();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _authenticationState.Clear();
                await EnsureAuthenticatedAsync();
                all = await _requestApiClient.GetAllRequestsAsync();
            }

            var currentUserId = _authenticationState.CurrentUser?.Id;

            AllRequests.Clear();
            UnassignedRequests.Clear();
            MyAssignedRequests.Clear();

            foreach (var item in all)
            {
                AllRequests.Add(item);

                if ((item.Status == RequestStatus.Created || item.Status == RequestStatus.Pending) && item.AssignedAgent == null)
                {
                    UnassignedRequests.Add(item);
                }

                if ((item.Status == RequestStatus.Claimed || item.Status == RequestStatus.InProgress) && item.AssignedAgent?.Id == currentUserId)
                {
                    MyAssignedRequests.Add(item);
                }
            }

            UnassignedCount = UnassignedRequests.Count;
            MyAssignedCount = MyAssignedRequests.Count;
            AllCount = AllRequests.Count;

            UpdateDisplayedQueue();

            StatusMessage = $"Queues updated ({UnassignedCount} pending, {MyAssignedCount} active)";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to refresh queues: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateDisplayedQueue()
    {
        DisplayedRequests.Clear();
        var source = SelectedQueueIndex switch
        {
            0 => UnassignedRequests,
            1 => MyAssignedRequests,
            _ => AllRequests
        };

        foreach (var req in source)
        {
            DisplayedRequests.Add(req);
        }

        // Auto-select first item if current selection not in view
        if (DisplayedRequests.Count > 0 && (SelectedRequest == null || !DisplayedRequests.Contains(SelectedRequest)))
        {
            SelectedRequest = DisplayedRequests[0];
        }
        else if (DisplayedRequests.Count == 0)
        {
            SelectedRequest = null;
        }
    }

    [RelayCommand]
    public async Task ClaimTicketAsync()
    {
        if (SelectedRequest == null) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            ConflictMessage = null;
            StatusMessage = $"Claiming ticket #{SelectedRequest.Id.ToString().Substring(0, 8)}...";

            await EnsureAuthenticatedAsync();

            SupportRequestDto claimed;
            try
            {
                claimed = await _requestApiClient.ClaimRequestAsync(SelectedRequest.Id);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _authenticationState.Clear();
                await EnsureAuthenticatedAsync();
                claimed = await _requestApiClient.ClaimRequestAsync(SelectedRequest.Id);
            }

            StatusMessage = $"Ticket #{claimed.Id.ToString().Substring(0, 8)} successfully claimed!";

            // Switch to My Active queue (Index 1) and refresh
            SelectedQueueIndex = 1;
            await RefreshQueuesAsync();
            SelectedRequest = DisplayedRequests.FirstOrDefault(r => r.Id == claimed.Id) ?? claimed;
        }
        catch (ConcurrencyConflictException ex)
        {
            // PostgreSQL xmin optimistic concurrency conflict caught!
            ConflictMessage = $"⚠️ Concurrency Conflict: {ex.Message} The ticket queue has been refreshed.";
            StatusMessage = "Claim conflict detected";
            await RefreshQueuesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to claim ticket: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ResolveTicketAsync()
    {
        if (SelectedRequest == null) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            ConflictMessage = null;
            StatusMessage = $"Resolving ticket #{SelectedRequest.Id.ToString().Substring(0, 8)}...";

            await EnsureAuthenticatedAsync();

            SupportRequestDto resolved;
            try
            {
                resolved = await _requestApiClient.ResolveRequestAsync(SelectedRequest.Id);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _authenticationState.Clear();
                await EnsureAuthenticatedAsync();
                resolved = await _requestApiClient.ResolveRequestAsync(SelectedRequest.Id);
            }

            StatusMessage = $"Ticket #{resolved.Id.ToString().Substring(0, 8)} marked as Resolved!";
            await RefreshQueuesAsync();
            SelectedRequest = resolved;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to resolve ticket: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task CloseTicketAsync()
    {
        if (SelectedRequest == null) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            ConflictMessage = null;
            StatusMessage = $"Closing ticket #{SelectedRequest.Id.ToString().Substring(0, 8)}...";

            await EnsureAuthenticatedAsync();

            SupportRequestDto closed;
            try
            {
                closed = await _requestApiClient.CloseRequestAsync(SelectedRequest.Id);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _authenticationState.Clear();
                await EnsureAuthenticatedAsync();
                closed = await _requestApiClient.CloseRequestAsync(SelectedRequest.Id);
            }

            StatusMessage = $"Ticket #{closed.Id.ToString().Substring(0, 8)} closed";
            await RefreshQueuesAsync();
            SelectedRequest = closed;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to close ticket: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
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
            ErrorMessage = $"Failed to send message: {ex.Message}";
        }
    }

    public async Task SendScreenshotAsync(string base64Data, string fileName = "screenshot.png")
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

    public async Task NotifyTypingAsync(bool isTyping)
    {
        if (SelectedRequest == null || !IsLiveChatActive) return;
        try
        {
            await _chatHubService.SendTypingAsync(SelectedRequest.Id, isTyping);
        }
        catch
        {
            // Ignore minor typing network glitches
        }
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
        UnassignedRequests.Clear();
        MyAssignedRequests.Clear();
        AllRequests.Clear();
        DisplayedRequests.Clear();
        LiveChatMessages.Clear();
        IsLiveChatActive = false;
        IsViewingChatHistory = false;
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

            if (!_authenticationState.IsAuthorizedAgent)
            {
                LoginErrorMessage = $"Access Restricted: User '{auth.User.DomainUsername}' with role '{auth.User.RoleName}' is not authorized to access the Customer Service Agent Console.";
                _authenticationState.Clear();
                return;
            }

            IsLoginViewVisible = false;
            LoginPassword = string.Empty;
            CurrentAgentDisplayName = auth.User.DisplayName;
            CurrentAgentDomain = auth.User.DomainUsername;
            CurrentAgentDepartment = auth.User.Department;
            CurrentAgentRole = auth.User.RoleName;
            IsAuthorizedAgent = _authenticationState.IsAuthorizedAgent;

            StatusMessage = $"Signed in as {auth.User.DisplayName} ({auth.User.RoleName})";
            await _chatHubService.ConnectAsync();
            await RefreshQueuesAsync();
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
        if (_authenticationState.IsAuthenticated && _authenticationState.IsAuthorizedAgent)
        {
            IsLoginViewVisible = false;
            LoginErrorMessage = null;
        }
    }

    [RelayCommand]
    public void ViewCustomerChatHistory()
    {
        if (SelectedRequest == null) return;

        HistoryChatMessages.Clear();
        var parsed = TriageTranscriptParser.Parse(SelectedRequest.Description);

        foreach (var (sender, content) in parsed.ChatMessages)
        {
            var isCustomer = sender.Equals("Customer", StringComparison.OrdinalIgnoreCase);
            var (cleanText, imageUrl) = ExtractAttachmentUrl(content);

            HistoryChatMessages.Add(new ChatMessageModel
            {
                Sender = isCustomer ? ChatSender.Customer : ChatSender.Assistant,
                Content = cleanText,
                ImageUrl = imageUrl,
                Timestamp = SelectedRequest.CreatedAt
            });
        }

        HistoryChatTitle = $"Customer Triage Dialogue — {SelectedRequest.DepartmentTitle}";
        HistoryChatSubtitle = $"Customer: {SelectedRequest.Requester?.DisplayName} ({SelectedRequest.Requester?.Department})";
        IsViewingChatHistory = true;
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
    public void CloseCustomerChatHistory()
    {
        IsViewingChatHistory = false;
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

