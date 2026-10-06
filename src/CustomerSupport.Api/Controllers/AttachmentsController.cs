using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.Api.Controllers;

public class Base64AttachmentUploadDto
{
    public string FileName { get; set; } = "screenshot.png";
    public string Base64Data { get; set; } = string.Empty;
}

[ApiController]
[Authorize]
[Route("api/attachments")]
public class AttachmentsController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AttachmentsController> _logger;

    public AttachmentsController(IWebHostEnvironment environment, ILogger<AttachmentsController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Uploads an image/screenshot attachment via multipart form data.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadForm([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded." });
        }

        var webRoot = _environment.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png";
        }

        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsDir, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var url = $"/uploads/{uniqueFileName}";
        _logger.LogInformation("Attachment uploaded successfully: {Url} ({Size} bytes)", url, file.Length);

        return Ok(new
        {
            url,
            fileName = uniqueFileName,
            size = file.Length
        });
    }

    /// <summary>
    /// Uploads an image/screenshot attachment via base64 JSON payload.
    /// </summary>
    [HttpPost("upload-base64")]
    [Consumes("application/json")]
    public async Task<IActionResult> UploadBase64([FromBody] Base64AttachmentUploadDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Base64Data))
        {
            return BadRequest(new { error = "Base64 data is required." });
        }

        var cleanBase64 = dto.Base64Data;
        if (cleanBase64.Contains(','))
        {
            cleanBase64 = cleanBase64.Split(',')[1];
        }

        byte[] fileBytes;
        try
        {
            fileBytes = Convert.FromBase64String(cleanBase64);
        }
        catch (FormatException)
        {
            return BadRequest(new { error = "Invalid base64 string provided." });
        }

        var webRoot = _environment.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var extension = Path.GetExtension(dto.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png";
        }

        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsDir, uniqueFileName);

        await System.IO.File.WriteAllBytesAsync(filePath, fileBytes, cancellationToken);

        var url = $"/uploads/{uniqueFileName}";
        _logger.LogInformation("Base64 attachment uploaded: {Url} ({Size} bytes)", url, fileBytes.Length);

        return Ok(new
        {
            url,
            fileName = uniqueFileName,
            size = fileBytes.Length
        });
    }
}
