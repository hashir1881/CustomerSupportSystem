using CustomerSupport.Application.Dtos;
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Services;

public interface ISupportRequestService
{
    Task<SupportRequestDto> CreateAsync(CreateSupportRequestDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportRequestDto>> GetAllAsync(RequestStatus? statusFilter = null, CancellationToken cancellationToken = default);
    Task<SupportRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupportRequestDto> UpdateAsync(Guid id, UpdateSupportRequestDto dto, CancellationToken cancellationToken = default);
    Task<SupportRequestDto> ClaimAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupportRequestDto> ResolveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupportRequestDto> CloseAsync(Guid id, CancellationToken cancellationToken = default);
}
