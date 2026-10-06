using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace CustomerSupport_CsDesktop.Models;

public enum ChatSender
{
    Assistant,
    Customer,
    Agent,
    System
}

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

    public bool IsCustomer => Sender == ChatSender.Customer;

    public bool IsAssistant => Sender == ChatSender.Assistant;

    public bool IsAgent => Sender == ChatSender.Agent;

    public bool IsMine { get; set; }

    public string SenderDisplayName { get; set; } = string.Empty;

    public string SenderName => !string.IsNullOrWhiteSpace(SenderDisplayName)
        ? SenderDisplayName
        : (IsCustomer ? "Customer" : (IsAgent ? "Support Agent" : "Virtual Assistant"));

    public bool IsSystemEvent { get; set; }

    public HorizontalAlignment Alignment => IsSystemEvent ? HorizontalAlignment.Center
        : (IsMine ? HorizontalAlignment.Right : HorizontalAlignment.Left);
}
