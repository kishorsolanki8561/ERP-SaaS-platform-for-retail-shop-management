using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Transport.Entities;

[Auditable("Transport.Vehicle")]
public class Vehicle : TenantEntity
{
    public string LicensePlate { get; set; } = default!;
    public string Model { get; set; } = default!;
    public decimal MaxLoadKg { get; set; }
    public long? TransportProviderId { get; set; }
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public bool IsActive { get; set; } = true;
    public TransportProvider? TransportProvider { get; set; }
    public ICollection<Delivery> Deliveries { get; set; } = [];
}
