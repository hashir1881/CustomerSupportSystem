using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Dtos;

public class UpdateSupportRequestDto
{
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RequestPriority Priority { get; set; } = RequestPriority.Medium;
}
