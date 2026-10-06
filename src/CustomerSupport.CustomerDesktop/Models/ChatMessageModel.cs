using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace CustomerSupport_CustomerDesktop.Models;

public enum ChatSender
{
    Assistant,
    User,
    Agent,
    System
}

/// <summary>
/// Represents a message in the conversational triage chat stream or live agent chat.
/// </summary>
public partial class ChatMessageModel : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ChatSender Sender { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public Microsoft.UI.Xaml.Media.Imaging.BitmapImage? ImageBitmap
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ImageUrl)) return null;
            try
            {
                return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(ImageUrl));
            }
            catch
            {
                return null;
            }
        }
    }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);

    public bool HasText => !string.IsNullOrWhiteSpace(Content);

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public string FormattedTime => Timestamp.LocalDateTime.ToString("t");

    public bool IsUser => Sender == ChatSender.User;

    public bool IsAssistant => Sender == ChatSender.Assistant;

    public bool IsAgent => Sender == ChatSender.Agent;

    public bool IsMine { get; set; } = true;

    public string SenderDisplayName { get; set; } = string.Empty;

    public string SenderName => !string.IsNullOrWhiteSpace(SenderDisplayName)
        ? SenderDisplayName
        : (IsUser ? "You" : (IsAgent ? "Support Agent" : "Support Assistant"));

    public bool IsSystemEvent { get; set; }

    public HorizontalAlignment Alignment => IsSystemEvent ? HorizontalAlignment.Center
        : ((IsUser || IsMine) && !IsAgent && !IsAssistant ? HorizontalAlignment.Right : HorizontalAlignment.Left);

    public IReadOnlyList<string>? QuickReplies { get; set; }

    public bool HasQuickReplies => QuickReplies != null && QuickReplies.Count > 0;

    public bool IsTicketCard { get; set; }

    public Guid? CreatedTicketId { get; set; }

    public string? CreatedTicketSubject { get; set; }

    public string? CreatedTicketStatus { get; set; }
}
