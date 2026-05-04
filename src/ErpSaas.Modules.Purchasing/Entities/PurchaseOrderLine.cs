using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Purchasing.Entities;

[Auditable("Purchasing.PurchaseOrderLine", ParentEntityType = "PurchaseOrder", ParentIdProperty = "PurchaseOrderId")]
public class PurchaseOrderLine : TenantEntity
{
    public long PurchaseOrderId { get; set; }
    public long ProductId { get; set; }

    [AuditField("Product Name")]
    public string ProductNameSnapshot { get; set; } = default!;

    [AuditField("Product Code")]
    public string ProductCodeSnapshot { get; set; } = default!;

    public string? HsnSacCodeSnapshot { get; set; }
    public long ProductUnitId { get; set; }

    [AuditField("Unit")]
    public string UnitCodeSnapshot { get; set; } = default!;

    public decimal ConversionFactorSnapshot { get; set; }

    [AuditField("Ordered Qty")]
    public decimal QuantityInBilledUnit { get; set; }

    public decimal QuantityInBaseUnit { get; set; }

    [AuditField("Received Qty")]
    public decimal QuantityReceived { get; set; }

    [AuditField("Unit Price")]
    public decimal UnitPrice { get; set; }

    [AuditField("Discount %")]
    public decimal DiscountPercent { get; set; }

    [AuditField("Discount Amount")]
    public decimal DiscountAmount { get; set; }

    [AuditField("Taxable Amount")]
    public decimal TaxableAmount { get; set; }

    [AuditField("GST Rate")]
    public decimal GstRate { get; set; }

    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }

    [AuditField("Line Total")]
    public decimal LineTotal { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = default!;
}
