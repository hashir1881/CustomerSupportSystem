using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Dtos;

public class UserDto
{
    public Guid Id { get; set; }
    public string DomainUsername { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string RoleName => Role.ToString();
}
