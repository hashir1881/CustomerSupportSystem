using CustomerSupport.Application.Services;
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Tests;

public class SupportTriageEngineTests
{
    private readonly SupportTriageEngine _engine = new();

    [Fact]
    public void InitialGreeting_ReturnsExpectedTextAndQuickReplies()
    {
        var greeting = _engine.GetInitialGreeting();

        Assert.Equal(TriageStep.Greeting, _engine.CurrentStep);
        Assert.Contains("IT Support Virtual Assistant", greeting.BotReply);
        Assert.NotNull(greeting.QuickReplies);
        Assert.Contains("Software not working", greeting.QuickReplies);
    }

    [Fact]
    public void TriageFlow_HMIS1_Cardiology_CompletesSuccessfullyWithReport()
    {
        // Act 1: General problem trigger
        var r1 = _engine.ProcessUserInput("I am facing problem in software");
        Assert.Equal(TriageStep.AwaitingSoftwareSelection, _engine.CurrentStep);
        Assert.Contains("which software", r1.BotReply, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(r1.QuickReplies);
        Assert.Contains("HMIS", r1.QuickReplies);

        // Act 2: Select HMIS -> should ask for version
        var r2 = _engine.ProcessUserInput("HMIS");
        Assert.Equal(TriageStep.AwaitingSoftwareVersion, _engine.CurrentStep);
        Assert.Contains("HMIS 1 or HMIS 2", r2.BotReply, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(r2.QuickReplies);
        Assert.Contains("HMIS 1", r2.QuickReplies);

        // Act 3: Select HMIS 1 -> should ask for department
        var r3 = _engine.ProcessUserInput("HMIS 1");
        Assert.Equal(TriageStep.AwaitingDepartment, _engine.CurrentStep);
        Assert.Contains("Department", r3.BotReply, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(r3.QuickReplies);
        Assert.Contains("Cardiology", r3.QuickReplies);

        // Act 4: Enter Department
        var r4 = _engine.ProcessUserInput("Cardiology");
        Assert.Equal(TriageStep.AwaitingIssueDescription, _engine.CurrentStep);
        Assert.Contains("Cardiology", r4.BotReply);
        Assert.Contains("describe the exact problem", r4.BotReply, StringComparison.OrdinalIgnoreCase);

        // Act 5: Enter specific issue
        var r5 = _engine.ProcessUserInput("Discharge summary is giving error 504 and failing to print.");
        Assert.Equal(TriageStep.Completed, _engine.CurrentStep);
        Assert.True(r5.IsReadyToSubmitTicket);
        Assert.NotNull(r5.TicketDto);

        // Verify compiled ticket details
        Assert.Equal("Cardiology", r5.TicketDto.Subject);
        Assert.Contains("AUTOMATED TRIAGE REPORT", r5.TicketDto.Description);
        Assert.Contains("CONVERSATIONAL TRIAGE TRANSCRIPT", r5.TicketDto.Description);
        Assert.Contains("[Customer]: Discharge summary is giving error 504", r5.TicketDto.Description);
        Assert.Equal(RequestPriority.Medium, r5.TicketDto.Priority);
    }

    [Fact]
    public void TriageFlow_HRMS_SkipsDepartmentSelection_AsRequestedByUser()
    {
        // Act 1: General problem trigger
        _engine.ProcessUserInput("Software not working");
        Assert.Equal(TriageStep.AwaitingSoftwareSelection, _engine.CurrentStep);

        // Act 2: User selects HRMS
        var r2 = _engine.ProcessUserInput("HRMS");

        // Per user requirement: HRMS should NOT ask department! Should directly go to AwaitingIssueDescription.
        Assert.Equal(TriageStep.AwaitingIssueDescription, _engine.CurrentStep);
        Assert.Equal("Human Resources (HR)", _engine.SelectedDepartment);
        Assert.Contains("Human Resources", r2.BotReply);
        Assert.Contains("describe the specific issue", r2.BotReply, StringComparison.OrdinalIgnoreCase);

        // Act 3: Enter issue with urgent keyword
        var r3 = _engine.ProcessUserInput("Urgent: Salary slip generation is crashing with out-of-memory error.");
        Assert.Equal(TriageStep.Completed, _engine.CurrentStep);
        Assert.True(r3.IsReadyToSubmitTicket);
        Assert.NotNull(r3.TicketDto);

        Assert.Equal("Human Resources (HR)", r3.TicketDto.Subject);
        Assert.Equal(RequestPriority.High, r3.TicketDto.Priority);
    }

    [Fact]
    public void TriageFlow_DirectSoftwareMention_ImmediatelyRoutesToVersion()
    {
        // Act: User directly types HMIS is not working
        var r1 = _engine.ProcessUserInput("HMIS software is not working at our counter");

        // Should jump straight to AwaitingSoftwareVersion
        Assert.Equal(TriageStep.AwaitingSoftwareVersion, _engine.CurrentStep);
        Assert.Equal("HMIS", _engine.SelectedSoftware);
        Assert.Contains("HMIS 1 or HMIS 2", r1.BotReply);
    }

    [Fact]
    public void Reset_ClearsAllConversationAndState()
    {
        _engine.ProcessUserInput("HMIS");
        Assert.NotEqual(TriageStep.Greeting, _engine.CurrentStep);

        _engine.Reset();

        Assert.Equal(TriageStep.Greeting, _engine.CurrentStep);
        Assert.Null(_engine.SelectedSoftware);
        Assert.Null(_engine.SelectedVersion);
        Assert.Null(_engine.SelectedDepartment);
        Assert.Empty(_engine.Transcript);
    }

    [Fact]
    public void TriageTranscriptParser_ParsesSummaryAndChatMessagesCorrectly()
    {
        // Arrange: Simulated generated ticket description
        var rawDescription = @"=== AUTOMATED TRIAGE REPORT ===
• Software System : HMIS 1
• Department      : Cardiology
• Issue Reported  : Discharge slip error 504

=== CONVERSATIONAL TRIAGE TRANSCRIPT ===
[Customer]: I am facing problem in software
[Assistant]: Which software are you using?
[Customer]: HMIS
[Assistant]: Which version of HMIS are you using? (HMIS 1 or HMIS 2)
[Customer]: HMIS 1
[Assistant]: Please specify your Department:
[Customer]: Cardiology
[Assistant]: Please describe the exact problem:
[Customer]: Discharge slip error 504";

        // Act
        var parsed = TriageTranscriptParser.Parse(rawDescription);

        // Assert
        Assert.True(parsed.HasTranscript);
        Assert.Contains("Software System : HMIS 1", parsed.Summary);
        Assert.DoesNotContain("CONVERSATIONAL TRIAGE TRANSCRIPT", parsed.Summary);
        Assert.Equal(9, parsed.ChatMessages.Count);
        Assert.Equal("Customer", parsed.ChatMessages[0].Sender);
        Assert.Equal("I am facing problem in software", parsed.ChatMessages[0].Content);
        Assert.Equal("Assistant", parsed.ChatMessages[1].Sender);
        Assert.Equal("Which software are you using?", parsed.ChatMessages[1].Content);
        Assert.Equal("Customer", parsed.ChatMessages[8].Sender);
        Assert.Equal("Discharge slip error 504", parsed.ChatMessages[8].Content);
    }

    [Fact]
    public void SupportRequestDto_SummaryText_ReturnsCleanSummaryWithoutRawTranscript()
    {
        var dto = new CustomerSupport.Application.Dtos.SupportRequestDto
        {
            Subject = "Test Subject",
            Description = @"=== AUTOMATED TRIAGE REPORT ===
• Software System : HMIS 2
• Department      : Emergency
• Issue Reported  : Patient record locked

=== CONVERSATIONAL TRIAGE TRANSCRIPT ===
[Customer]: Software issue
[Assistant]: Which software?
[Customer]: HMIS 2"
        };

        Assert.True(dto.HasTranscript);
        Assert.Contains("Software System : HMIS 2", dto.SummaryText);
        Assert.DoesNotContain("CONVERSATIONAL TRIAGE TRANSCRIPT", dto.SummaryText);
        Assert.DoesNotContain("AUTOMATED TRIAGE REPORT", dto.SummaryText);
    }
}
