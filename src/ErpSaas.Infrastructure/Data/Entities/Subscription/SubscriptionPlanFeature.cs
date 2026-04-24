using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Subscription;

public class SubscriptionPlanFeature : BaseEntity
{
    public long PlanId { get; set; }
    public string FeatureCode { get; set; } = "";

    public SubscriptionPlan Plan { get; set; } = null!;
}
