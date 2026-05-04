using ErpSaas.Modules.SalesReturns.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.SalesReturns.Entities;

[Auditable("SalesReturns.SalesReturn")]
public class SalesReturn : TenantEntity
{
    [AuditField("Return Number")]
    public string ReturnNumber { get; set; } = default!;

    public long InvoiceId { get; set; }

    [AuditField("Invoice Number")]
    public string InvoiceNumberSnapshot { get; set; } = default!;

    public long CustomerId { get; set; }

    [AuditField("Customer Name")]
    public string CustomerNameSnapshot { get; set; } = default!;

    [AuditField("Return Date")]
    public DateTime ReturnDate { get; set; }

    [AuditField("Status")]
    public SalesReturnStatus Status { get; set; } = SalesReturnStatus.Draft;

    [AuditField("Refund Method")]
    public RefundMethod RefundMethod { get; set; }

    [AuditField("Total Refund Amount")]
    public decimal TotalRefundAmount { get; set; }

    [AuditField("Refunded to Wallet")]
    public decimal? RefundedToWallet { get; set; }

    [AuditField("Refunded to Cash")]
    public decimal? RefundedToCash { get; set; }

    [AuditField("Reason")]
    public string? Reason { get; set; }

    public long? CreditNoteId { get; set; }

    public ICollection<SalesReturnLine> Lines { get; set; } = [];
    public CreditNote? CreditNote { get; set; }
}
