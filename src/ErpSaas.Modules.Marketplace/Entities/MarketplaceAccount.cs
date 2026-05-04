using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Marketplace.Entities;

[Auditable("Marketplace.Account")]
public class MarketplaceAccount : TenantEntity
{
    public string MarketplaceCode { get; set; } = default!;
    public string AccountName { get; set; } = default!;
    public string SellerId { get; set; } = default!;
    public string CredentialsJsonEncrypted { get; set; } = default!;
    public bool SyncInventory { get; set; }
    public bool SyncPricing { get; set; }
    public bool SyncOrders { get; set; }
    public DateTime? LastSyncUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MarketplaceProductMapping> ProductMappings { get; set; } = [];
    public ICollection<MarketplaceOrder> Orders { get; set; } = [];
}
