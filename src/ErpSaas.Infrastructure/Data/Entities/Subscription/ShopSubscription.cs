using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Subscription;

public enum BillingCycle { Monthly, Annual }

public class ShopSubscription : BaseEntity
{
    public long ShopId { get; set; }
    public long PlanId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public BillingCycle BillingCycle { get; set; }

    public Identity.Shop Shop { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
}
