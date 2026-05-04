using ErpSaas.Modules.Purchasing.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Purchasing.Entities;

[Auditable("Purchasing.PurchaseOrder")]
public class PurchaseOrder : TenantEntity
{
    [AuditField("PO Number")]
    public string PoNumber { get; set; } = default!;

    public long SupplierId { get; set; }

    [AuditField("Supplier Name")]
    public string SupplierNameSnapshot { get; set; } = default!;

    public string? SupplierGstSnapshot { get; set; }

    [AuditField("Order Date")]
    public DateTime OrderDate { get; set; }

    [AuditField("Expected Delivery Date")]
    public DateTime? ExpectedDeliveryDate { get; set; }

    [AuditField("Status")]
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    [AuditField("Sub Total")]
    public decimal SubTotal { get; set; }

    [AuditField("Total Tax")]
    public decimal TotalTaxAmount { get; set; }

    [AuditField("Grand Total")]
    public decimal GrandTotal { get; set; }

    [AuditField("Notes")]
    public string? Notes { get; set; }

    public long? BranchId { get; set; }

    public Supplier Supplier { get; set; } = default!;
    public ICollection<PurchaseOrderLine> Lines { get; set; } = [];
    public ICollection<Bill> Bills { get; set; } = [];
}
