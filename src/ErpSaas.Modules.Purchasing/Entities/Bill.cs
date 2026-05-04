using ErpSaas.Modules.Purchasing.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Purchasing.Entities;

[Auditable("Purchasing.Bill")]
public class Bill : TenantEntity
{
    [AuditField("Bill Number")]
    public string BillNumber { get; set; } = default!;

    [AuditField("Supplier Bill Number")]
    public string? SupplierBillNumber { get; set; }

    public long SupplierId { get; set; }

    [AuditField("Supplier Name")]
    public string SupplierNameSnapshot { get; set; } = default!;

    public long? PurchaseOrderId { get; set; }

    [AuditField("Bill Date")]
    public DateTime BillDate { get; set; }

    [AuditField("Due Date")]
    public DateTime? DueDate { get; set; }

    [AuditField("Status")]
    public BillStatus Status { get; set; } = BillStatus.Draft;

    [AuditField("Sub Total")]
    public decimal SubTotal { get; set; }

    [AuditField("Total Tax")]
    public decimal TotalTaxAmount { get; set; }

    [AuditField("Grand Total")]
    public decimal GrandTotal { get; set; }

    [AuditField("Paid Amount")]
    public decimal PaidAmount { get; set; }

    [AuditField("Outstanding Amount")]
    public decimal OutstandingAmount { get; set; }

    [AuditField("Notes")]
    public string? Notes { get; set; }

    public Supplier Supplier { get; set; } = default!;
    public PurchaseOrder? PurchaseOrder { get; set; }
    public ICollection<BillPayment> Payments { get; set; } = [];
}
