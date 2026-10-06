using CustomerSupport.Application.Common.Exceptions;
using CustomerSupport.Application.Common.Interfaces;
using CustomerSupport.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Services;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UserService(IApplicationDbContext _context, ICurrentUserService currentUserService)
    {
        this._context = _context;
        _currentUserService = currentUserService;
    }

    public async Task<UserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new ForbiddenException("No authenticated user was identified.");
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.DomainUsername == _currentUserService.DomainUsername, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", _currentUserService.DomainUsername ?? "unknown");
        }

        return new UserDto
        {
            Id = user.Id,
            DomainUsername = user.DomainUsername,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Department = user.Department,
            Role = user.Role
        };
    }
}
