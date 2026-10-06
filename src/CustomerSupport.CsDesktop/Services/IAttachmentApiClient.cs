using System.Net.Http.Json;
using System.Text.Json;

namespace CustomerSupport_CsDesktop.Services;

public class AttachmentUploadResult
{
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
}

public interface IAttachmentApiClient
{
    Task<string> UploadScreenshotBase64Async(string base64Data, string fileName = "screenshot.png", CancellationToken cancellationToken = default);
}

public class AttachmentApiClient : IAttachmentApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AttachmentApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> UploadScreenshotBase64Async(string base64Data, string fileName = "screenshot.png", CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            FileName = fileName,
            Base64Data = base64Data
        };

        var response = await _httpClient.PostAsJsonAsync("api/attachments/upload-base64", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AttachmentUploadResult>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize attachment upload response.");

        return result.Url;
    }
}
