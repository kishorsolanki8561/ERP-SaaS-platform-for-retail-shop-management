using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.ApiAccess.Services;

public record RegisterEndpointDto(string Name, string Url, string EventsCsv, int MaxRetries = 5,
    int TimeoutSeconds = 10, string? CustomHeadersJson = null);
public record UpdateEndpointDto(string Name, string Url, string EventsCsv, bool IsActive,
    int MaxRetries = 5, int TimeoutSeconds = 10, string? CustomHeadersJson = null);
public record EndpointListItem(long Id, string Name, string Url, string EventsCsv, bool IsActive,
    int MaxRetries, int TimeoutSeconds, DateTime CreatedAtUtc);
public record DeliveryListItem(long Id, long EndpointId, string EventCode, Guid DeliveryId,
    int AttemptNumber, int? ResponseStatusCode, string Status, string? ErrorMessage,
    DateTime? DeliveredAtUtc, DateTime CreatedAtUtc);

public interface IWebhookDispatchService
{
    Task DispatchAsync(long shopId, string eventCode, object payload, CancellationToken ct = default);
    Task<Result<EndpointListItem>> RegisterEndpointAsync(RegisterEndpointDto dto, CancellationToken ct = default);
    Task<Result<bool>> UpdateEndpointAsync(long id, UpdateEndpointDto dto, CancellationToken ct = default);
    Task<Result<string>> RotateSecretAsync(long id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<EndpointListItem>>> ListEndpointsAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<DeliveryListItem>>> ListDeliveriesAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<Result<bool>> RetryDeliveryAsync(long deliveryId, CancellationToken ct = default);
    IReadOnlyList<string> GetEventCatalog();
}
