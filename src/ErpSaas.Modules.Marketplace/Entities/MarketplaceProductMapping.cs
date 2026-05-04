using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Marketplace.Entities;

public class MarketplaceProductMapping : TenantEntity
{
    public long MarketplaceAccountId { get; set; }
    public long ProductId { get; set; }
    public long? ProductVariantId { get; set; }
    public string MarketplaceSku { get; set; } = default!;
    public string MarketplaceListingId { get; set; } = default!;
    public decimal? PriceOverride { get; set; }
    public bool IsActive { get; set; } = true;

    public MarketplaceAccount Account { get; set; } = default!;
}
