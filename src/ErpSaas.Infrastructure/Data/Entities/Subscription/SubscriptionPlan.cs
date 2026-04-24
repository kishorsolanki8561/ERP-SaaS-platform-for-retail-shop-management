using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Subscription;

public class SubscriptionPlan : BaseEntity
{
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public int MaxUsers { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SubscriptionPlanFeature> Features { get; set; } = [];
    public ICollection<ShopSubscription> ShopSubscriptions { get; set; } = [];
}
