using ErpSaas.Modules.Purchasing.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Purchasing.Entities;

[Auditable("Purchasing.PurchaseOrder")]
public class PurchaseOrder : TenantEntity
{
    public string PoNumber { get; set; } = default!;
    public long SupplierId { get; set; }
    public string SupplierNameSnapshot { get; set; } = default!;
    public string? SupplierGstSnapshot { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string? Notes { get; set; }
    public long? BranchId { get; set; }

    public Supplier Supplier { get; set; } = default!;
    public ICollection<PurchaseOrderLine> Lines { get; set; } = [];
    public ICollection<Bill> Bills { get; set; } = [];
}
