using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Billing.Entities;

[Auditable("Billing.InvoiceLine", ParentEntityType = "Invoice", ParentIdProperty = "InvoiceId")]
public class InvoiceLine : TenantEntity
{
    public long InvoiceId { get; set; }
    public long ProductId { get; set; }

    [AuditField("Product Name")]
    public string ProductNameSnapshot { get; set; } = "";

    [AuditField("Product Code")]
    public string ProductCodeSnapshot { get; set; } = "";

    public string? HsnSacCodeSnapshot { get; set; }

    public long ProductUnitId { get; set; }

    [AuditField("Unit")]
    public string UnitCodeSnapshot { get; set; } = "";

    public decimal ConversionFactorSnapshot { get; set; } = 1m;

    [AuditField("Quantity")]
    public decimal QuantityInBilledUnit { get; set; }

    public decimal QuantityInBaseUnit { get; set; }

    [AuditField("Unit Price")]
    public decimal UnitPrice { get; set; }

    [AuditField("Discount %")]
    public decimal DiscountPercent { get; set; } = 0m;

    [AuditField("Discount Amount")]
    public decimal DiscountAmount { get; set; } = 0m;

    [AuditField("Taxable Amount")]
    public decimal TaxableAmount { get; set; }

    [AuditField("GST Rate")]
    public decimal GstRate { get; set; }

    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }

    [AuditField("Line Total")]
    public decimal LineTotal { get; set; }

    public int SortOrder { get; set; }

    public Invoice Invoice { get; set; } = null!;
}
