using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CustomerSupport.Application.Dtos;
using CustomerSupport.Domain.Enums;

namespace CustomerSupport_CsDesktop.Services;

public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message) : base(message) { }
}

public interface ICsSupportRequestApiClient
{
    Task<IReadOnlyList<SupportRequestDto>> GetAllRequestsAsync(RequestStatus? status = null, CancellationToken cancellationToken = default);
    Task<SupportRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupportRequestDto> ClaimRequestAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupportRequestDto> ResolveRequestAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupportRequestDto> CloseRequestAsync(Guid id, CancellationToken cancellationToken = default);
}

public class CsSupportRequestApiClient : ICsSupportRequestApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CsSupportRequestApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<SupportRequestDto>> GetAllRequestsAsync(RequestStatus? status = null, CancellationToken cancellationToken = default)
    {
        var uri = status.HasValue ? $"api/support-requests?status={status.Value}" : "api/support-requests";
        var response = await _httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<IReadOnlyList<SupportRequestDto>>(JsonOptions, cancellationToken);
        return list ?? Array.Empty<SupportRequestDto>();
    }

    public async Task<SupportRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/support-requests/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var request = await response.Content.ReadFromJsonAsync<SupportRequestDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Failed to deserialize support request {id}.");

        return request;
    }

    public async Task<SupportRequestDto> ClaimRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/support-requests/{id}/claim", null, cancellationToken);

        // Detect PostgreSQL xmin optimistic concurrency conflict (409 Conflict)
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new ConcurrencyConflictException("This ticket has already been claimed by another support agent.");
        }

        response.EnsureSuccessStatusCode();

        var claimed = await response.Content.ReadFromJsonAsync<SupportRequestDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Failed to deserialize claimed support request {id}.");

        return claimed;
    }

    public async Task<SupportRequestDto> ResolveRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/support-requests/{id}/resolve", null, cancellationToken);
        response.EnsureSuccessStatusCode();

        var resolved = await response.Content.ReadFromJsonAsync<SupportRequestDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Failed to deserialize resolved support request {id}.");

        return resolved;
    }

    public async Task<SupportRequestDto> CloseRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/support-requests/{id}/close", null, cancellationToken);
        response.EnsureSuccessStatusCode();

        var closed = await response.Content.ReadFromJsonAsync<SupportRequestDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Failed to deserialize closed support request {id}.");

        return closed;
    }
}
