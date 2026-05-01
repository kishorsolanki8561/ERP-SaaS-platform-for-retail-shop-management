using ErpSaas.Modules.Quotations.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Quotations.Entities;

[Auditable("Quotations.Quotation")]
public class Quotation : TenantEntity
{
    public string QuotationNumber { get; set; } = default!;
    public long CustomerId { get; set; }
    public string CustomerNameSnapshot { get; set; } = default!;
    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;
    public DateTime QuotationDate { get; set; }
    public DateTime ValidUntil { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }
    public string? Notes { get; set; }
    public long? BranchId { get; set; }
    public long? ConvertedToSalesOrderId { get; set; }
    public ICollection<QuotationLine> Lines { get; set; } = [];
}
