using ErpSaas.Modules.Quotations.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Quotations.Entities;

[Auditable("Quotations.DeliveryChallan")]
public class DeliveryChallan : TenantEntity
{
    public string DcNumber { get; set; } = default!;
    public long SalesOrderId { get; set; }
    public long? InvoiceId { get; set; }
    public DeliveryChallanStatus Status { get; set; } = DeliveryChallanStatus.Draft;
    public DateTime ChallanDate { get; set; }
    public DateTime? DispatchedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? TransporterName { get; set; }
    public string? VehicleNumber { get; set; }
    public string? Notes { get; set; }
    public long? BranchId { get; set; }
    public SalesOrder SalesOrder { get; set; } = default!;
    public ICollection<DeliveryChallanLine> Lines { get; set; } = [];
}
