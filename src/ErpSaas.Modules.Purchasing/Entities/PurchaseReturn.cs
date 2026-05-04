using ErpSaas.Modules.Purchasing.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Purchasing.Entities;

[Auditable("Purchasing.PurchaseReturn")]
public class PurchaseReturn : TenantEntity
{
    [AuditField("Return Number")]
    public string ReturnNumber { get; set; } = default!;

    public long SupplierId { get; set; }

    [AuditField("Supplier Name")]
    public string SupplierNameSnapshot { get; set; } = default!;

    public long? PurchaseOrderId { get; set; }

    [AuditField("PO Number")]
    public string? PoNumberSnapshot { get; set; }

    [AuditField("Return Date")]
    public DateTime ReturnDate { get; set; }

    [AuditField("Status")]
    public PurchaseReturnStatus Status { get; set; } = PurchaseReturnStatus.Draft;

    [AuditField("Sub Total")]
    public decimal SubTotal { get; set; }

    [AuditField("Total Tax")]
    public decimal TotalTaxAmount { get; set; }

    [AuditField("Grand Total")]
    public decimal GrandTotal { get; set; }

    [AuditField("Reason")]
    public string? Reason { get; set; }

    [AuditField("Notes")]
    public string? Notes { get; set; }

    public long? DebitNoteId { get; set; }
    public long? BranchId { get; set; }

    public Supplier Supplier { get; set; } = default!;
    public ICollection<PurchaseReturnLine> Lines { get; set; } = [];
    public DebitNote? DebitNote { get; set; }
}
