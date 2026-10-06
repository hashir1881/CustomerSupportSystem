using System.ComponentModel;
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Dtos;

public class SupportRequestDto : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private int _unreadCount;

    public int UnreadCount
    {
        get => _unreadCount;
        set
        {
            if (_unreadCount != value)
            {
                _unreadCount = value;
                OnPropertyChanged(nameof(UnreadCount));
                OnPropertyChanged(nameof(HasUnreadMessages));
            }
        }
    }

    public bool HasUnreadMessages => _unreadCount > 0;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public Guid Id { get; set; }
    public string IdString => Id.ToString();
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RequestStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public RequestPriority Priority { get; set; }
    public string PriorityName => Priority.ToString();

    public UserDto Requester { get; set; } = null!;
    public UserDto? AssignedAgent { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    public string CreatedAtFormatted => CreatedAt.LocalDateTime.ToString("g");
    public string? ClaimedAtFormatted => ClaimedAt?.LocalDateTime.ToString("g");
    public string? ResolvedAtFormatted => ResolvedAt?.LocalDateTime.ToString("g");
    public string? ClosedAtFormatted => ClosedAt?.LocalDateTime.ToString("g");
    public bool CanBeClosed => Status != RequestStatus.Closed;

    public Guid? ConversationId { get; set; }

    /// <summary>
    /// Displays only the department name, stripping any legacy "[Software - Department] Complain" formats.
    /// </summary>
    public string DepartmentTitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Subject))
            {
                var match = System.Text.RegularExpressions.Regex.Match(Subject, @"\[[^-\]]+-\s*([^\]]+)\]");
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            if (!string.IsNullOrWhiteSpace(Description))
            {
                var match = System.Text.RegularExpressions.Regex.Match(Description, @"•\s*Department\s*:\s*([^\r\n]+)");
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            return !string.IsNullOrWhiteSpace(Subject) ? Subject : (Requester?.Department ?? "General");
        }
    }

    /// <summary>
    /// Extracts the relative URL of an error screenshot attached during triage, if any.
    /// </summary>
    public string? AttachmentUrl
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Description)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(Description, @"\[Screenshot:\s*([^\]\r\n]+)\]");
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
    }

    /// <summary>
    /// Fully qualified HTTP URL for the screenshot attachment.
    /// </summary>
    public string? FullAttachmentUrl
    {
        get
        {
            if (string.IsNullOrWhiteSpace(AttachmentUrl)) return null;
            return AttachmentUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? AttachmentUrl
                : $"http://localhost:5239{AttachmentUrl}";
        }
    }

    public bool HasAttachment => !string.IsNullOrWhiteSpace(AttachmentUrl);

    public string SummaryText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Description)) return string.Empty;
            const string marker = "=== CONVERSATIONAL TRIAGE TRANSCRIPT ===";
            var idx = Description.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            var text = idx >= 0 ? Description.Substring(0, idx).Trim() : Description;
            text = text.Replace("=== AUTOMATED TRIAGE REPORT ===", "").Trim();

            // Replace raw [Screenshot: /uploads/...png] User attached error screenshot: ... with "File Attached"
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\[Screenshot:\s*[^\]\r\n]+\]\s*(User attached error screenshot:\s*[^\r\n]*)?",
                "File Attached",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            ).Trim();

            return text;
        }
    }

    public bool HasTranscript => !string.IsNullOrWhiteSpace(Description) && Description.Contains("=== CONVERSATIONAL TRIAGE TRANSCRIPT ===", StringComparison.OrdinalIgnoreCase);
}
