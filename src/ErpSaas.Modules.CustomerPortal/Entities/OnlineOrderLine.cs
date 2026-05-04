using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.CustomerPortal.Entities;

public sealed class OnlineOrderLine : TenantEntity
{
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = default!;
    public long? ProductUnitId { get; set; }
    public string UnitCodeSnapshot { get; set; } = default!;
    public decimal ConversionFactorSnapshot { get; set; } = 1m;
    public decimal QuantityInBilledUnit { get; set; }
    public decimal QuantityInBaseUnit { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public decimal DiscountSnapshot { get; set; }
    public decimal GstRateSnapshot { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal GstAmount { get; set; }
    public decimal LineTotal { get; set; }

    public OnlineOrder Order { get; set; } = null!;
}
