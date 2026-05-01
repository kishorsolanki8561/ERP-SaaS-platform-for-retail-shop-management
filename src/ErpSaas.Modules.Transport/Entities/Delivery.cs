using ErpSaas.Modules.Transport.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Transport.Entities;

[Auditable("Transport.Delivery")]
public class Delivery : TenantEntity
{
    public string DeliveryNumber { get; set; } = default!;
    public DeliveryReferenceType ReferenceType { get; set; }
    public long ReferenceId { get; set; }
    public string ReferenceNumberSnapshot { get; set; } = default!;
    public long CustomerId { get; set; }
    public string CustomerNameSnapshot { get; set; } = default!;
    public long? VehicleId { get; set; }
    public long? TransportProviderId { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Scheduled;
    public DateTime ScheduledDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public string DeliveryAddress { get; set; } = default!;
    public string? Notes { get; set; }
    public long? BranchId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public TransportProvider? TransportProvider { get; set; }
    public ICollection<DeliveryLog> Logs { get; set; } = [];
}
