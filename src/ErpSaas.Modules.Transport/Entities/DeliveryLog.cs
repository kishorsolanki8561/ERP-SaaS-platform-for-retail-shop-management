using ErpSaas.Modules.Transport.Enums;
using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Transport.Entities;

public class DeliveryLog : BaseEntity
{
    public long DeliveryId { get; set; }
    public DeliveryStatus Status { get; set; }
    public string? Notes { get; set; }
    public long LoggedByUserId { get; set; }
    public Delivery Delivery { get; set; } = default!;
}
