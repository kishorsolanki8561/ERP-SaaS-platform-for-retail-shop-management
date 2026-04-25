using ErpSaas.Infrastructure.Data;
using ErpSaas.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Identity.Services;

internal sealed class ShopInfoProvider(PlatformDbContext db) : IShopInfoProvider
{
    public async Task<ShopInfoSnapshot?> GetAsync(long shopId, CancellationToken ct = default)
    {
        var shop = await db.Shops
            .Where(s => s.Id == shopId && !s.IsDeleted)
            .Select(s => new ShopInfoSnapshot(
                s.LegalName,
                s.TradeName,
                s.GstNumber,
                s.AddressLine1,
                s.AddressLine2,
                s.City,
                s.StateCode,
                s.PinCode,
                null,
                null))
            .FirstOrDefaultAsync(ct);

        return shop;
    }
}
