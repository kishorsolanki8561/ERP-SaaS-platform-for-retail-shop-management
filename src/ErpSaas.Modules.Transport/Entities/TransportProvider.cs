using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Transport.Entities;

[Auditable("Transport.TransportProvider")]
public class TransportProvider : TenantEntity
{
    public string Name { get; set; } = default!;
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? GstNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Delivery> Deliveries { get; set; } = [];
}
