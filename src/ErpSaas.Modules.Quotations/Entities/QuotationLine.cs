using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Quotations.Entities;

public class QuotationLine : BaseEntity
{
    public long QuotationId { get; set; }
    public long ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = default!;
    public long ProductUnitId { get; set; }
    public string UnitCodeSnapshot { get; set; } = default!;
    public decimal ConversionFactorSnapshot { get; set; }
    public decimal QuantityInBilledUnit { get; set; }
    public decimal QuantityInBaseUnit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal GstRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public Quotation Quotation { get; set; } = default!;
}
