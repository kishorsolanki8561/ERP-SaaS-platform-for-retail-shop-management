using ErpSaas.Modules.Quotations.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Quotations.Entities;

[Auditable("Quotations.Quotation")]
public class Quotation : TenantEntity
{
    [AuditField("Quotation Number")]
    public string QuotationNumber { get; set; } = default!;

    public long CustomerId { get; set; }

    [AuditField("Customer Name")]
    public string CustomerNameSnapshot { get; set; } = default!;

    [AuditField("Status")]
    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;

    [AuditField("Quotation Date")]
    public DateTime QuotationDate { get; set; }

    [AuditField("Valid Until")]
    public DateTime ValidUntil { get; set; }

    [AuditField("Sub Total")]
    public decimal SubTotal { get; set; }

    [AuditField("Total Discount")]
    public decimal TotalDiscount { get; set; }

    [AuditField("Total Tax")]
    public decimal TotalTax { get; set; }

    [AuditField("Grand Total")]
    public decimal GrandTotal { get; set; }

    [AuditField("Notes")]
    public string? Notes { get; set; }

    public long? BranchId { get; set; }
    public long? ConvertedToSalesOrderId { get; set; }

    public ICollection<QuotationLine> Lines { get; set; } = [];
}
