using System.Net.Http.Json;
using System.Text.Json;
using CustomerSupport.Application.Dtos;

namespace CustomerSupport_CustomerDesktop.Services;

public interface ISupportRequestApiClient
{
    Task<IReadOnlyList<SupportRequestDto>> GetMyRequestsAsync(CancellationToken cancellationToken = default);
    Task<SupportRequestDto> CreateRequestAsync(CreateSupportRequestDto dto, CancellationToken cancellationToken = default);
    Task<SupportRequestDto> CloseRequestAsync(Guid id, CancellationToken cancellationToken = default);
}

public class SupportRequestApiClient : ISupportRequestApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SupportRequestApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<SupportRequestDto>> GetMyRequestsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/support-requests", cancellationToken);
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<IReadOnlyList<SupportRequestDto>>(JsonOptions, cancellationToken);
        return list ?? Array.Empty<SupportRequestDto>();
    }

    public async Task<SupportRequestDto> CreateRequestAsync(CreateSupportRequestDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/support-requests", dto, cancellationToken);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<SupportRequestDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize created support request.");

        return created;
    }

    public async Task<SupportRequestDto> CloseRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/support-requests/{id}/close", null, cancellationToken);
        response.EnsureSuccessStatusCode();

        var closed = await response.Content.ReadFromJsonAsync<SupportRequestDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize closed support request.");

        return closed;
    }
}
