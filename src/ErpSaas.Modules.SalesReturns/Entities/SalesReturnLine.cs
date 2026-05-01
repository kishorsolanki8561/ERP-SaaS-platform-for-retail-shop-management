using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.SalesReturns.Entities;

public class SalesReturnLine : TenantEntity
{
    public long SalesReturnId { get; set; }
    public long InvoiceLineId { get; set; }
    public long ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = default!;
    public string ProductCodeSnapshot { get; set; } = default!;
    public long ProductUnitId { get; set; }
    public string UnitCodeSnapshot { get; set; } = default!;
    public decimal ConversionFactorSnapshot { get; set; }
    public decimal QuantityInBilledUnit { get; set; }
    public decimal QuantityInBaseUnit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal GstRate { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal LineTotal { get; set; }

    public SalesReturn SalesReturn { get; set; } = default!;
}
