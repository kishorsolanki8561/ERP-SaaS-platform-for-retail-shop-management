using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Quotations.Entities;

[Auditable("Quotations.QuotationLine", ParentEntityType = "Quotation", ParentIdProperty = "QuotationId")]
public class QuotationLine : BaseEntity
{
    public long QuotationId { get; set; }
    public long ProductId { get; set; }

    [AuditField("Product Name")]
    public string ProductNameSnapshot { get; set; } = default!;

    public long ProductUnitId { get; set; }

    [AuditField("Unit")]
    public string UnitCodeSnapshot { get; set; } = default!;

    public decimal ConversionFactorSnapshot { get; set; }

    [AuditField("Quantity")]
    public decimal QuantityInBilledUnit { get; set; }

    public decimal QuantityInBaseUnit { get; set; }

    [AuditField("Unit Price")]
    public decimal UnitPrice { get; set; }

    [AuditField("Discount Amount")]
    public decimal DiscountAmount { get; set; }

    [AuditField("Taxable Amount")]
    public decimal TaxableAmount { get; set; }

    [AuditField("GST Rate")]
    public decimal GstRate { get; set; }

    [AuditField("Tax Amount")]
    public decimal TaxAmount { get; set; }

    [AuditField("Line Total")]
    public decimal LineTotal { get; set; }

    public Quotation Quotation { get; set; } = default!;
}
