using CustomerSupport.Application.Dtos;

namespace CustomerSupport.Application.Services;

public interface IUserService
{
    Task<UserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
