using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Subscription;

public class SubscriptionPlan : BaseEntity
{
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public int MaxUsers { get; set; }
    public int MaxProducts { get; set; } = 500;
    public int MaxInvoicesPerMonth { get; set; } = 1000;
    public int StorageQuotaMb { get; set; } = 500;
    public int SmsQuotaPerMonth { get; set; } = 100;
    public int EmailQuotaPerMonth { get; set; } = 500;
    public bool IsActive { get; set; } = true;

    public ICollection<SubscriptionPlanFeature> Features { get; set; } = [];
    public ICollection<ShopSubscription> ShopSubscriptions { get; set; } = [];
}
