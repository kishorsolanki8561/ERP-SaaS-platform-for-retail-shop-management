using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.ServiceJobs.Entities;

public class ServiceJobPart : TenantEntity
{
    public long ServiceJobId { get; set; }
    public long ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineCost { get; set; }
    public long? StockMovementId { get; set; }

    public ServiceJob Job { get; set; } = default!;
}
