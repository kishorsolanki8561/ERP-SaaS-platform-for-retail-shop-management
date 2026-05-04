using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Verticals.Grocery.Entities;

public class LoyaltyProgram : TenantEntity
{
    public string Name { get; set; } = default!;
    public decimal PointsPerRupee { get; set; } = 1m;
    public decimal RupeeValuePerPoint { get; set; } = 0.25m;
    public decimal MinimumRedemptionPoints { get; set; } = 100m;
    public decimal MaxRedemptionPercentPerBill { get; set; } = 20m;
    public int PointExpiryDays { get; set; } = 365;
    public bool IsActive { get; set; } = true;
}
