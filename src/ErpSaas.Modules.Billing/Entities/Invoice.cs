using ErpSaas.Modules.Billing.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Billing.Entities;

[Auditable("Invoice")]
public class Invoice : TenantEntity
{
    [AuditField("Invoice Number")]
    public string InvoiceNumber { get; set; } = "";

    [AuditField("Invoice Date")]
    public DateTime InvoiceDate { get; set; }

    public long CustomerId { get; set; }

    [AuditField("Customer Name")]
    public string CustomerNameSnapshot { get; set; } = "";

    public string? CustomerGstSnapshot { get; set; }

    public string? CustomerPhoneSnapshot { get; set; }

    public string? BillingAddressSnapshot { get; set; }

    [AuditField("Status")]
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    [AuditField("Sub Total")]
    public decimal SubTotal { get; set; }

    [AuditField("Total Discount")]
    public decimal TotalDiscount { get; set; } = 0m;

    [AuditField("Total Tax")]
    public decimal TotalTaxAmount { get; set; }

    public decimal RoundOff { get; set; } = 0m;

    [AuditField("Grand Total")]
    public decimal GrandTotal { get; set; }

    [AuditField("Notes")]
    public string? Notes { get; set; }

    [AuditField("Payment Terms")]
    public string? PaymentTerms { get; set; }

    [AuditField("Due Date")]
    public DateTime? DueDate { get; set; }

    [AuditField("Paid Amount")]
    public decimal PaidAmount { get; set; }

    [AuditField("Outstanding Amount")]
    public decimal OutstandingAmount { get; set; }

    public long WarehouseId { get; set; }
    public long? ShiftId { get; set; }
    public long? BranchId { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = [];
    public ICollection<InvoicePayment> Payments { get; set; } = [];
}
