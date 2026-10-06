namespace CustomerSupport.Application.Services;

public class ParsedTriageData
{
    public string Summary { get; set; } = string.Empty;
    public IReadOnlyList<(string Sender, string Content)> ChatMessages { get; set; } = Array.Empty<(string, string)>();
    public bool HasTranscript => ChatMessages.Count > 0;
}

public static class TriageTranscriptParser
{
    private const string TranscriptHeader = "=== CONVERSATIONAL TRIAGE TRANSCRIPT ===";
    private const string ReportHeader = "=== AUTOMATED TRIAGE REPORT ===";

    public static ParsedTriageData Parse(string? rawDescription)
    {
        if (string.IsNullOrWhiteSpace(rawDescription))
        {
            return new ParsedTriageData();
        }

        var result = new ParsedTriageData();
        var transcriptIndex = rawDescription.IndexOf(TranscriptHeader, StringComparison.OrdinalIgnoreCase);

        if (transcriptIndex >= 0)
        {
            // Extract summary part
            var summaryPart = rawDescription.Substring(0, transcriptIndex).Trim();
            result.Summary = summaryPart.Replace(ReportHeader, "").Trim();

            // Extract transcript part
            var transcriptContent = rawDescription.Substring(transcriptIndex + TranscriptHeader.Length).Trim();
            var lines = transcriptContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            var messages = new List<(string Sender, string Content)>();
            string? currentSender = null;
            var currentContent = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("[Customer]:", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentSender != null)
                    {
                        messages.Add((currentSender, currentContent.ToString().Trim()));
                        currentContent.Clear();
                    }
                    currentSender = "Customer";
                    currentContent.Append(trimmed.Substring("[Customer]:".Length).Trim());
                }
                else if (trimmed.StartsWith("[Assistant]:", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentSender != null)
                    {
                        messages.Add((currentSender, currentContent.ToString().Trim()));
                        currentContent.Clear();
                    }
                    currentSender = "Assistant";
                    currentContent.Append(trimmed.Substring("[Assistant]:".Length).Trim());
                }
                else if (currentSender != null)
                {
                    currentContent.AppendLine().Append(trimmed);
                }
            }

            if (currentSender != null && currentContent.Length > 0)
            {
                messages.Add((currentSender, currentContent.ToString().Trim()));
            }

            result.ChatMessages = messages;
        }
        else
        {
            // No transcript header found; the whole description is the summary
            result.Summary = rawDescription.Trim();
        }

        return result;
    }
}
