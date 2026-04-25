#pragma warning disable CS9107
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Identity.Services;

public sealed class AdminService(
    PlatformDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant)
    : BaseService<PlatformDbContext>(db, errorLogger), IAdminService
{
    public Task<ShopProfileDto?> GetShopProfileAsync(CancellationToken ct = default)
        => db.Shops
            .Where(s => s.Id == tenant.ShopId && !s.IsDeleted)
            .Select(s => (ShopProfileDto?)new ShopProfileDto(
                s.ShopCode, s.LegalName, s.TradeName, s.GstNumber,
                s.AddressLine1, s.AddressLine2, s.City,
                s.StateCode, s.PinCode, s.CurrencyCode, s.TimeZone))
            .FirstOrDefaultAsync(ct);

    public async Task<Result<bool>> UpdateShopProfileAsync(
        UpdateShopProfileDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Admin.UpdateShopProfile", async () =>
        {
            var shop = await db.Shops
                .FirstOrDefaultAsync(s => s.Id == tenant.ShopId && !s.IsDeleted, ct);

            if (shop is null)
                return Result<bool>.NotFound(Errors.Admin.ShopNotFound);

            shop.LegalName    = dto.LegalName;
            shop.TradeName    = dto.TradeName;
            shop.GstNumber    = dto.GstNumber;
            shop.AddressLine1 = dto.AddressLine1;
            shop.AddressLine2 = dto.AddressLine2;
            shop.City         = dto.City;
            shop.StateCode    = dto.StateCode;
            shop.PinCode      = dto.PinCode;
            shop.CurrencyCode = dto.CurrencyCode;

            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<PagedResult<AdminUserDto>> ListUsersAsync(
        int pageNumber, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = db.UserShops
            .Where(us => us.ShopId == tenant.ShopId && us.IsActive && !us.User.IsDeleted)
            .Select(us => us.User);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.DisplayName.Contains(search) ||
                (u.Email != null && u.Email.Contains(search)) ||
                (u.Phone != null && u.Phone.Contains(search)));

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.DisplayName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto(u.Id, u.DisplayName, u.Email, u.Phone, u.IsActive))
            .ToListAsync(ct);

        return new PagedResult<AdminUserDto>(items, totalCount, pageNumber, pageSize);
    }
}
