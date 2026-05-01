using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Purchasing.Entities;

public class PurchaseReturnLine : TenantEntity
{
    public long PurchaseReturnId { get; set; }
    public long ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = default!;
    public string ProductCodeSnapshot { get; set; } = default!;
    public string? HsnSacCodeSnapshot { get; set; }

    // §3.7 unit snapshot fields
    public long ProductUnitId { get; set; }
    public string UnitCodeSnapshot { get; set; } = default!;
    public decimal ConversionFactorSnapshot { get; set; }
    public decimal QuantityInBilledUnit { get; set; }
    public decimal QuantityInBaseUnit { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal GstRate { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal LineTotal { get; set; }

    public PurchaseReturn PurchaseReturn { get; set; } = default!;
}
