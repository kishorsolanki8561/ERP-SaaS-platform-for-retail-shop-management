using ErpSaas.Modules.Billing.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Billing.Entities;

[Auditable("Invoice")]
public class Invoice : TenantEntity
{
    /// <summary>From ISequenceService — unique per shop.</summary>
    public string InvoiceNumber { get; set; } = "";

    public DateTime InvoiceDate { get; set; }

    public long CustomerId { get; set; }

    /// <summary>Snapshot of customer name at time of invoicing.</summary>
    public string CustomerNameSnapshot { get; set; } = "";

    public string? CustomerGstSnapshot { get; set; }

    /// <summary>Snapshot of customer phone at time of invoicing — used for SMS on finalize.</summary>
    public string? CustomerPhoneSnapshot { get; set; }

    public string? BillingAddressSnapshot { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public decimal SubTotal { get; set; }

    public decimal TotalDiscount { get; set; } = 0m;

    public decimal TotalTaxAmount { get; set; }

    public decimal RoundOff { get; set; } = 0m;

    public decimal GrandTotal { get; set; }

    public string? Notes { get; set; }

    /// <summary>Wholesale credit terms label (e.g. "NET30"). Null for retail cash invoices.</summary>
    public string? PaymentTerms { get; set; }

    /// <summary>Due date for payment — computed from PaymentTerms at finalization.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Sum of all InvoicePayment rows recorded against this invoice.</summary>
    public decimal PaidAmount { get; set; }

    /// <summary>GrandTotal minus PaidAmount. Negative values indicate overpayment.</summary>
    public decimal OutstandingAmount { get; set; }

    public long WarehouseId { get; set; }

    /// <summary>Null for wholesale invoices created outside POS; required for retail POS invoices.</summary>
    public long? ShiftId { get; set; }

    /// <summary>Branch where the invoice was issued.</summary>
    public long? BranchId { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = [];
    public ICollection<InvoicePayment> Payments { get; set; } = [];
}
