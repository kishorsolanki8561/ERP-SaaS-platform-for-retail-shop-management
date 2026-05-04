using ErpSaas.Shared.Data;
using ErpSaas.Modules.ApiAccess.Enums;

namespace ErpSaas.Modules.ApiAccess.Entities;

public sealed class WebhookDelivery : TenantEntity
{
    public long WebhookEndpointId { get; set; }
    public string EventCode { get; set; } = default!;
    public Guid DeliveryId { get; set; }
    public string PayloadJson { get; set; } = default!;
    public int AttemptNumber { get; set; }
    public int? ResponseStatusCode { get; set; }
    public int? ResponseTimeMs { get; set; }
    public string? ResponseBody { get; set; }
    public WebhookDeliveryStatus Status { get; set; } = WebhookDeliveryStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }

    public WebhookEndpoint Endpoint { get; set; } = default!;
}
