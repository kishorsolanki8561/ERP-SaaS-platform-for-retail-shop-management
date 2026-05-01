using ErpSaas.Modules.SalesReturns.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.SalesReturns.Entities;

[Auditable("SalesReturns.SalesReturn")]
public class SalesReturn : TenantEntity
{
    public string ReturnNumber { get; set; } = default!;
    public long InvoiceId { get; set; }
    public string InvoiceNumberSnapshot { get; set; } = default!;
    public long CustomerId { get; set; }
    public string CustomerNameSnapshot { get; set; } = default!;
    public DateTime ReturnDate { get; set; }
    public SalesReturnStatus Status { get; set; } = SalesReturnStatus.Draft;
    public RefundMethod RefundMethod { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public string? Reason { get; set; }
    public long? CreditNoteId { get; set; }

    public ICollection<SalesReturnLine> Lines { get; set; } = [];
    public CreditNote? CreditNote { get; set; }
}
