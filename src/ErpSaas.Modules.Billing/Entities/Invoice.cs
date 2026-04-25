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

    public string? BillingAddressSnapshot { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public decimal SubTotal { get; set; }

    public decimal TotalDiscount { get; set; } = 0m;

    public decimal TotalTaxAmount { get; set; }

    public decimal RoundOff { get; set; } = 0m;

    public decimal GrandTotal { get; set; }

    public string? Notes { get; set; }

    public long WarehouseId { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = [];
}
