namespace CustomerSupport.Domain.Enums;

/// <summary>
/// Type of message inside a support conversation.
/// </summary>
public enum MessageType
{
    Text = 1,
    SystemNotification = 2,
    StatusChange = 3,
    Image = 4
}
