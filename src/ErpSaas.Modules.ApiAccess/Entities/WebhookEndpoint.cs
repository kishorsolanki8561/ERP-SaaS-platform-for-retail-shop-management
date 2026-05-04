using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.ApiAccess.Entities;

[Auditable("ApiAccess.WebhookEndpoint")]
public sealed class WebhookEndpoint : TenantEntity
{
    public string Url { get; set; } = default!;
    public string SigningSecret { get; set; } = default!;
    public string EventsCsv { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
    public int MaxRetries { get; set; } = 5;
    public int TimeoutSeconds { get; set; } = 10;
    public string? CustomHeadersJson { get; set; }

    public ICollection<WebhookDelivery> Deliveries { get; set; } = [];
}
