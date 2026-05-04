using ErpSaas.Modules.Quotations.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Quotations.Entities;

[Auditable("Quotations.DeliveryChallan")]
public class DeliveryChallan : TenantEntity
{
    [AuditField("DC Number")]
    public string DcNumber { get; set; } = default!;

    public long SalesOrderId { get; set; }
    public long? InvoiceId { get; set; }

    [AuditField("Status")]
    public DeliveryChallanStatus Status { get; set; } = DeliveryChallanStatus.Draft;

    [AuditField("Challan Date")]
    public DateTime ChallanDate { get; set; }

    [AuditField("Dispatched Date")]
    public DateTime? DispatchedDate { get; set; }

    [AuditField("Delivered Date")]
    public DateTime? DeliveredDate { get; set; }

    [AuditField("Delivery Address")]
    public string? DeliveryAddress { get; set; }

    [AuditField("Transporter Name")]
    public string? TransporterName { get; set; }

    [AuditField("Vehicle Number")]
    public string? VehicleNumber { get; set; }

    [AuditField("Notes")]
    public string? Notes { get; set; }

    public long? BranchId { get; set; }

    public SalesOrder SalesOrder { get; set; } = default!;
    public ICollection<DeliveryChallanLine> Lines { get; set; } = [];
}
