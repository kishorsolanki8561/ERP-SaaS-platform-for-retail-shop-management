using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Identity;

public class Shop : BaseEntity
{
    public string ShopCode { get; set; } = "";
    public string LegalName { get; set; } = "";
    public string? TradeName { get; set; }
    public string? GstNumber { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateCode { get; set; }
    public string? PinCode { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public string TimeZone { get; set; } = "Asia/Kolkata";
    public bool IsActive { get; set; } = true;

    public ICollection<UserShop> UserShops { get; set; } = [];
    public ICollection<ShopSubscription> Subscriptions { get; set; } = [];
}
