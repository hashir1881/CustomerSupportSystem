using System.Text;
using CustomerSupport.Application.Dtos;
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Services;

public enum TriageStep
{
    Greeting,
    AwaitingSoftwareSelection,
    AwaitingSoftwareVersion,
    AwaitingDepartment,
    AwaitingIssueDescription,
    Completed
}

public class TriageResult
{
    public string BotReply { get; set; } = string.Empty;
    public IReadOnlyList<string>? QuickReplies { get; set; }
    public bool IsReadyToSubmitTicket { get; set; }
    public CreateSupportRequestDto? TicketDto { get; set; }
}

public interface ISupportTriageEngine
{
    TriageStep CurrentStep { get; }
    string? SelectedSoftware { get; }
    string? SelectedVersion { get; }
    string? SelectedDepartment { get; }
    string? IssueDescription { get; }
    IReadOnlyList<(string Sender, string Message)> Transcript { get; }

    void Reset();
    TriageResult GetInitialGreeting();
    TriageResult ProcessUserInput(string input);
}

/// <summary>
/// Domain application service managing conversational triage and auto-generating tickets.
/// Handles branching for software (HMIS, TRIAGE, HRMS, LIS), versions, and optional department logic.
/// </summary>
public class SupportTriageEngine : ISupportTriageEngine
{
    public TriageStep CurrentStep { get; private set; } = TriageStep.Greeting;

    public string? SelectedSoftware { get; private set; }
    public string? SelectedVersion { get; private set; }
    public string? SelectedDepartment { get; private set; }
    public string? IssueDescription { get; private set; }

    private readonly List<(string Sender, string Message)> _transcript = new();
    public IReadOnlyList<(string Sender, string Message)> Transcript => _transcript.AsReadOnly();

    public void Reset()
    {
        CurrentStep = TriageStep.Greeting;
        SelectedSoftware = null;
        SelectedVersion = null;
        SelectedDepartment = null;
        IssueDescription = null;
        _transcript.Clear();
    }

    public TriageResult GetInitialGreeting()
    {
        Reset();
        var greetingText = "Hello! I am your IT Support Virtual Assistant. How can I assist you today?\n\nYou can describe your problem below (for example: \"Software not working\" or \"Facing problem in software\").";
        _transcript.Add(("Assistant", greetingText));

        return new TriageResult
        {
            BotReply = greetingText,
            QuickReplies = new[] { "Software not working", "Facing problem in software", "HMIS issue", "HRMS issue" }
        };
    }

    public TriageResult ProcessUserInput(string input)
    {
        var cleanInput = input?.Trim() ?? string.Empty;
        _transcript.Add(("Customer", cleanInput));

        var result = new TriageResult();

        switch (CurrentStep)
        {
            case TriageStep.Greeting:
                result = HandleGreetingOrGeneralIssue(cleanInput);
                break;

            case TriageStep.AwaitingSoftwareSelection:
                result = HandleSoftwareSelection(cleanInput);
                break;

            case TriageStep.AwaitingSoftwareVersion:
                result = HandleSoftwareVersion(cleanInput);
                break;

            case TriageStep.AwaitingDepartment:
                result = HandleDepartment(cleanInput);
                break;

            case TriageStep.AwaitingIssueDescription:
                result = HandleIssueDescription(cleanInput);
                break;

            case TriageStep.Completed:
                result.BotReply = "This ticket has already been generated. If you have another issue, click 'Start New Support Chat' above.";
                result.QuickReplies = new[] { "Start New Support Chat" };
                break;
        }

        if (!string.IsNullOrWhiteSpace(result.BotReply))
        {
            _transcript.Add(("Assistant", result.BotReply));
        }

        return result;
    }

    private TriageResult HandleGreetingOrGeneralIssue(string input)
    {
        var lower = input.ToLowerInvariant();

        // Direct mention of HMIS
        if (lower.Contains("hmis"))
        {
            SelectedSoftware = "HMIS";
            CurrentStep = TriageStep.AwaitingSoftwareVersion;
            return new TriageResult
            {
                BotReply = "I see you are having an issue with HMIS. Which version of HMIS are you using? (HMIS 1 or HMIS 2)",
                QuickReplies = new[] { "HMIS 1", "HMIS 2" }
            };
        }

        // Direct mention of HRMS - department is skipped per user requirement
        if (lower.Contains("hrms") || lower.Contains("human resource"))
        {
            SelectedSoftware = "HRMS";
            SelectedDepartment = "Human Resources (HR)";
            CurrentStep = TriageStep.AwaitingIssueDescription;
            return new TriageResult
            {
                BotReply = "You have selected HRMS. Since this belongs to Human Resources, department selection is automatic.\n\nPlease describe the specific issue or error you are experiencing:",
                QuickReplies = null
            };
        }

        if (lower.Contains("triage"))
        {
            SelectedSoftware = "TRIAGE";
            CurrentStep = TriageStep.AwaitingDepartment;
            return new TriageResult
            {
                BotReply = "You have selected TRIAGE. Please specify your Department:",
                QuickReplies = new[] { "Emergency", "OPD", "Triage Desk", "Trauma" }
            };
        }

        if (lower.Contains("lis") || lower.Contains("lab"))
        {
            SelectedSoftware = "LIS";
            CurrentStep = TriageStep.AwaitingDepartment;
            return new TriageResult
            {
                BotReply = "You have selected LIS (Laboratory Information System). Please specify your Department:",
                QuickReplies = new[] { "Pathology", "Hematology", "Biochemistry", "Microbiology" }
            };
        }

        // General software trigger
        CurrentStep = TriageStep.AwaitingSoftwareSelection;
        return new TriageResult
        {
            BotReply = "I understand you are experiencing an issue with hospital software. Can you please tell which software you are using?",
            QuickReplies = new[] { "HMIS", "TRIAGE", "HRMS", "LIS" }
        };
    }

    private TriageResult HandleSoftwareSelection(string input)
    {
        var lower = input.ToLowerInvariant();

        if (lower.Contains("hmis"))
        {
            SelectedSoftware = "HMIS";
            CurrentStep = TriageStep.AwaitingSoftwareVersion;
            return new TriageResult
            {
                BotReply = "There are two versions of HMIS in use. Could you please specify whether you are on HMIS 1 or HMIS 2?",
                QuickReplies = new[] { "HMIS 1", "HMIS 2" }
            };
        }

        if (lower.Contains("hrms") || lower.Contains("human resource"))
        {
            SelectedSoftware = "HRMS";
            // User requirement: For HRMS, department is not asked as it defaults to HR
            SelectedDepartment = "Human Resources (HR)";
            CurrentStep = TriageStep.AwaitingIssueDescription;
            return new TriageResult
            {
                BotReply = "Understood. For HRMS, department selection is automatically set to Human Resources.\n\nPlease describe the specific issue or error message you are encountering:",
                QuickReplies = null
            };
        }

        if (lower.Contains("triage"))
        {
            SelectedSoftware = "TRIAGE";
            CurrentStep = TriageStep.AwaitingDepartment;
            return new TriageResult
            {
                BotReply = "Understood. Please specify your Department for the TRIAGE system:",
                QuickReplies = new[] { "Emergency", "OPD", "Triage Desk", "Trauma" }
            };
        }

        if (lower.Contains("lis") || lower.Contains("lab"))
        {
            SelectedSoftware = "LIS";
            CurrentStep = TriageStep.AwaitingDepartment;
            return new TriageResult
            {
                BotReply = "Understood. Please specify your Department for LIS:",
                QuickReplies = new[] { "Pathology", "Hematology", "Biochemistry", "Microbiology" }
            };
        }

        // Fallback software
        SelectedSoftware = input;
        CurrentStep = TriageStep.AwaitingDepartment;
        return new TriageResult
        {
            BotReply = $"Got it: {SelectedSoftware}. Please specify your Department (e.g., Cardiology, Emergency, Radiology, Pharmacy, HR):",
            QuickReplies = new[] { "Cardiology", "Emergency", "Radiology", "Pharmacy", "ICU", "OPD" }
        };
    }

    private TriageResult HandleSoftwareVersion(string input)
    {
        var lower = input.ToLowerInvariant();
        if (lower.Contains("1"))
        {
            SelectedVersion = "HMIS 1";
        }
        else if (lower.Contains("2"))
        {
            SelectedVersion = "HMIS 2";
        }
        else
        {
            SelectedVersion = input.Trim();
        }

        CurrentStep = TriageStep.AwaitingDepartment;
        return new TriageResult
        {
            BotReply = $"Thank you. You selected {SelectedVersion}.\n\nPlease specify your Department:",
            QuickReplies = new[] { "Cardiology", "Emergency", "Radiology", "Pharmacy", "ICU", "OPD" }
        };
    }

    private TriageResult HandleDepartment(string input)
    {
        SelectedDepartment = input.Trim();
        CurrentStep = TriageStep.AwaitingIssueDescription;

        var systemName = !string.IsNullOrEmpty(SelectedVersion) ? SelectedVersion : SelectedSoftware;

        return new TriageResult
        {
            BotReply = $"Department recorded as '{SelectedDepartment}'.\n\nPlease describe the exact problem, error code, or issue you are experiencing with {systemName}:",
            QuickReplies = null
        };
    }

    private TriageResult HandleIssueDescription(string input)
    {
        IssueDescription = input.Trim();
        CurrentStep = TriageStep.Completed;

        var systemName = !string.IsNullOrEmpty(SelectedVersion) ? SelectedVersion : (SelectedSoftware ?? "Software");
        var department = !string.IsNullOrEmpty(SelectedDepartment) ? SelectedDepartment : "General";

        // Auto-detect priority
        var priority = RequestPriority.Medium;
        var lowerIssue = IssueDescription.ToLowerInvariant();
        if (lowerIssue.Contains("urgent") || lowerIssue.Contains("crash") || lowerIssue.Contains("critical") || lowerIssue.Contains("down") || lowerIssue.Contains("emergency"))
        {
            priority = RequestPriority.High;
        }

        // Subject now displays only the Department as requested (removing messy complain text)
        var subject = !string.IsNullOrWhiteSpace(department) ? department : "General";

        // Build comprehensive structured description + transcript
        var sb = new StringBuilder();
        sb.AppendLine("=== AUTOMATED TRIAGE REPORT ===");
        sb.AppendLine($"• Software System : {systemName}");
        sb.AppendLine($"• Department      : {department}");
        sb.AppendLine($"• Issue Reported  : {IssueDescription}");
        sb.AppendLine();
        sb.AppendLine("=== CONVERSATIONAL TRIAGE TRANSCRIPT ===");
        foreach (var entry in _transcript)
        {
            sb.AppendLine($"[{entry.Sender}]: {entry.Message}");
        }

        var dto = new CreateSupportRequestDto
        {
            Subject = subject,
            Description = sb.ToString(),
            Priority = priority
        };

        return new TriageResult
        {
            BotReply = "Thank you! I have compiled all your details. Creating your support ticket in the system now...",
            IsReadyToSubmitTicket = true,
            TicketDto = dto,
            QuickReplies = null
        };
    }
}
