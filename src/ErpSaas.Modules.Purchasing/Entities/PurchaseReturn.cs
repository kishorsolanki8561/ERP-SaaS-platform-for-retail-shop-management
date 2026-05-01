using ErpSaas.Modules.Purchasing.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Purchasing.Entities;

[Auditable("Purchasing.PurchaseReturn")]
public class PurchaseReturn : TenantEntity
{
    public string ReturnNumber { get; set; } = default!;
    public long SupplierId { get; set; }
    public string SupplierNameSnapshot { get; set; } = default!;
    public long? PurchaseOrderId { get; set; }
    public string? PoNumberSnapshot { get; set; }
    public DateTime ReturnDate { get; set; }
    public PurchaseReturnStatus Status { get; set; } = PurchaseReturnStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public long? DebitNoteId { get; set; }
    public long? BranchId { get; set; }

    public Supplier Supplier { get; set; } = default!;
    public ICollection<PurchaseReturnLine> Lines { get; set; } = [];
    public DebitNote? DebitNote { get; set; }
}
