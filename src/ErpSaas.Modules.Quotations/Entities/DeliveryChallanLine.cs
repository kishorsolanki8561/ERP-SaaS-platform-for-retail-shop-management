using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Quotations.Entities;

public class DeliveryChallanLine : BaseEntity
{
    public long DeliveryChallanId { get; set; }
    public long ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = default!;
    public long ProductUnitId { get; set; }
    public string UnitCodeSnapshot { get; set; } = default!;
    public decimal ConversionFactorSnapshot { get; set; }
    public decimal QuantityInBilledUnit { get; set; }
    public decimal QuantityInBaseUnit { get; set; }
    public DeliveryChallan DeliveryChallan { get; set; } = default!;
}
