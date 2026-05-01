using ErpSaas.Modules.SalesReturns.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.SalesReturns.Entities;

[Auditable("SalesReturns.CreditNote")]
public class CreditNote : TenantEntity
{
    public string CreditNoteNumber { get; set; } = default!;
    public long CustomerId { get; set; }
    public string CustomerNameSnapshot { get; set; } = default!;
    public long? SalesReturnId { get; set; }
    public long? OriginalInvoiceId { get; set; }
    public string? OriginalInvoiceNumberSnapshot { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public CreditNoteStatus Status { get; set; } = CreditNoteStatus.Draft;
    public decimal Amount { get; set; }
    public decimal UsedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string? Notes { get; set; }

    public ICollection<SalesReturn> SalesReturns { get; set; } = [];
}
