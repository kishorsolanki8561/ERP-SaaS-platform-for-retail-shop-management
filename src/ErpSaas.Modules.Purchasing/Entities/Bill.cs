using ErpSaas.Modules.Purchasing.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Purchasing.Entities;

[Auditable("Purchasing.Bill")]
public class Bill : TenantEntity
{
    public string BillNumber { get; set; } = default!;
    public string? SupplierBillNumber { get; set; }
    public long SupplierId { get; set; }
    public string SupplierNameSnapshot { get; set; } = default!;
    public long? PurchaseOrderId { get; set; }
    public DateTime BillDate { get; set; }
    public DateTime? DueDate { get; set; }
    public BillStatus Status { get; set; } = BillStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public string? Notes { get; set; }

    public Supplier Supplier { get; set; } = default!;
    public PurchaseOrder? PurchaseOrder { get; set; }
    public ICollection<BillPayment> Payments { get; set; } = [];
}
