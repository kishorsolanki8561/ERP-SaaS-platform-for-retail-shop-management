using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Quotations.Entities;

[Auditable("Quotations.DeliveryChallanLine", ParentEntityType = "DeliveryChallan", ParentIdProperty = "DeliveryChallanId")]
public class DeliveryChallanLine : BaseEntity
{
    public long DeliveryChallanId { get; set; }
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

    public DeliveryChallan DeliveryChallan { get; set; } = default!;
}
