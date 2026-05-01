using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Accounting.Entities;

public class DepreciationEntry : TenantEntity
{
    public long FixedAssetId { get; set; }
    public DateTime PeriodDate { get; set; }
    public decimal Amount { get; set; }
    public decimal AccumulatedAfter { get; set; }
    public decimal NetBookValueAfter { get; set; }
    public long VoucherId { get; set; }

    public FixedAsset FixedAsset { get; set; } = default!;
}
