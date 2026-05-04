using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.SalesReturns.Entities;

[Auditable("SalesReturns.SalesReturnLine", ParentEntityType = "SalesReturn", ParentIdProperty = "SalesReturnId")]
public class SalesReturnLine : TenantEntity
{
    public long SalesReturnId { get; set; }
    public long InvoiceLineId { get; set; }
    public long ProductId { get; set; }

    [AuditField("Product Name")]
    public string ProductNameSnapshot { get; set; } = default!;

    [AuditField("Product Code")]
    public string ProductCodeSnapshot { get; set; } = default!;

    public long ProductUnitId { get; set; }

    [AuditField("Unit")]
    public string UnitCodeSnapshot { get; set; } = default!;

    public decimal ConversionFactorSnapshot { get; set; }

    [AuditField("Quantity")]
    public decimal QuantityInBilledUnit { get; set; }

    public decimal QuantityInBaseUnit { get; set; }

    [AuditField("Unit Price")]
    public decimal UnitPrice { get; set; }

    [AuditField("Taxable Amount")]
    public decimal TaxableAmount { get; set; }

    [AuditField("GST Rate")]
    public decimal GstRate { get; set; }

    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }

    [AuditField("Line Total")]
    public decimal LineTotal { get; set; }

    public SalesReturn SalesReturn { get; set; } = default!;
}
