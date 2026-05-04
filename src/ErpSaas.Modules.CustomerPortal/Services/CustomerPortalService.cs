using Dapper;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.CustomerPortal.Services;

#pragma warning disable CS9107, CS9113
public sealed class CustomerPortalService(
    PlatformDbContext platformDb,
    TenantDbContext tenantDb,
    IErrorLogger errorLogger,
    ILogger<CustomerPortalService> logger)
    : BaseService<PlatformDbContext>(platformDb, errorLogger), ICustomerPortalService
{
    public async Task<Result<CustomerProfileDto>> GetProfileAsync(long platformCustomerId, CancellationToken ct = default)
    {
        return await ExecuteAsync("CustomerPortal.GetProfile", async () =>
        {
            var customer = await platformDb.PlatformCustomers.FindAsync([platformCustomerId], ct);
            if (customer is null)
                return Result<CustomerProfileDto>.NotFound("Customer not found");

            return Result<CustomerProfileDto>.Success(new CustomerProfileDto(
                customer.Id, customer.DisplayName, customer.Email, customer.Phone,
                customer.CreatedAtUtc, customer.LastLoginAtUtc));
        }, ct);
    }

    public async Task<Result<bool>> UpdateProfileAsync(long platformCustomerId, UpdateCustomerProfileDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("CustomerPortal.UpdateProfile", async () =>
        {
            var customer = await platformDb.PlatformCustomers.FindAsync([platformCustomerId], ct);
            if (customer is null)
                return Result<bool>.NotFound("Customer not found");

            if (dto.DisplayName is not null)
                customer.DisplayName = dto.DisplayName;
            if (dto.Email is not null)
                customer.Email = dto.Email;

            await platformDb.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<PagedResult<PurchaseHistoryDto>> ListPurchasesAsync(long platformCustomerId, int page, int pageSize, CancellationToken ct = default)
    {
        var shopIds = await platformDb.CustomerLinks
            .Where(l => l.PlatformCustomerId == platformCustomerId && l.IsActive)
            .Select(l => new { l.ShopId, l.TenantCustomerId })
            .ToListAsync(ct);

        if (shopIds.Count == 0)
            return new PagedResult<PurchaseHistoryDto>([], 0, page, pageSize);

        var sql = @"
            SELECT i.Id AS InvoiceId, i.InvoiceNumber, i.ShopId,
                   s.TradeName AS ShopName, i.InvoiceDate, i.GrandTotal, i.Status
            FROM sales.Invoice i
            JOIN [platform].[dbo].Shop s ON s.Id = i.ShopId
            WHERE i.CustomerId IN @CustomerIds
              AND i.ShopId IN @ShopIds
            ORDER BY i.InvoiceDate DESC
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        var countSql = @"
            SELECT COUNT(1) FROM sales.Invoice i
            WHERE i.CustomerId IN @CustomerIds AND i.ShopId IN @ShopIds";

        var customerIds = shopIds.Select(x => x.TenantCustomerId).Distinct().ToArray();
        var shopIdList = shopIds.Select(x => x.ShopId).Distinct().ToArray();

        var conn = tenantDb.Database.GetDbConnection();
        var rows = (await conn.QueryAsync<PurchaseHistoryDto>(sql, new
        {
            CustomerIds = customerIds,
            ShopIds = shopIdList,
            Skip = (page - 1) * pageSize,
            Take = pageSize,
        })).ToList();

        var total = await conn.ExecuteScalarAsync<int>(countSql, new { CustomerIds = customerIds, ShopIds = shopIdList });
        return new PagedResult<PurchaseHistoryDto>(rows, total, page, pageSize);
    }

    public async Task<Result<PurchaseDetailDto?>> GetPurchaseAsync(long platformCustomerId, long invoiceId, CancellationToken ct = default)
    {
        return await ExecuteAsync("CustomerPortal.GetPurchase", async () =>
        {
            var links = await platformDb.CustomerLinks
                .Where(l => l.PlatformCustomerId == platformCustomerId && l.IsActive)
                .Select(l => l.TenantCustomerId)
                .ToListAsync(ct);

            if (links.Count == 0)
                return Result<PurchaseDetailDto?>.Success(null);

            var sql = @"
                SELECT i.Id AS InvoiceId, i.InvoiceNumber, s.TradeName AS ShopName,
                       i.InvoiceDate, i.SubTotal, i.GrandTotal,
                       il.ProductNameSnapshot AS ProductName, il.UnitCodeSnapshot AS UnitCode,
                       il.QuantityInBilledUnit AS Qty, il.UnitPriceSnapshot AS UnitPrice, il.LineTotal
                FROM sales.Invoice i
                JOIN [platform].[dbo].Shop s ON s.Id = i.ShopId
                JOIN sales.InvoiceLine il ON il.InvoiceId = i.Id
                WHERE i.Id = @InvoiceId AND i.CustomerId IN @CustomerIds";

            var conn = tenantDb.Database.GetDbConnection();
            var rows = (await conn.QueryAsync(sql, new { InvoiceId = invoiceId, CustomerIds = links })).ToList();

            if (rows.Count == 0)
                return Result<PurchaseDetailDto?>.Success(null);

            var first = rows[0];
            var lines = rows.Select(r => new PurchaseLineDto(
                (string)r.ProductName, (string)r.UnitCode,
                (decimal)r.Qty, (decimal)r.UnitPrice, (decimal)r.LineTotal)).ToList();

            return Result<PurchaseDetailDto?>.Success(new PurchaseDetailDto(
                (long)first.InvoiceId, (string)first.InvoiceNumber, (string)first.ShopName,
                (DateTime)first.InvoiceDate, (decimal)first.SubTotal, (decimal)first.GrandTotal, lines));
        }, ct);
    }

    public async Task<Result<CustomerInsightsDto>> GetInsightsAsync(long platformCustomerId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await ExecuteAsync("CustomerPortal.GetInsights", async () =>
        {
            var links = await platformDb.CustomerLinks
                .Where(l => l.PlatformCustomerId == platformCustomerId && l.IsActive)
                .ToListAsync(ct);

            if (links.Count == 0)
                return Result<CustomerInsightsDto>.Success(new CustomerInsightsDto(0, 0, []));

            var customerIds = links.Select(l => l.TenantCustomerId).Distinct().ToArray();
            var shopIds = links.Select(l => l.ShopId).Distinct().ToArray();

            var sql = @"
                SELECT i.ShopId, s.TradeName AS ShopName,
                       SUM(i.GrandTotal) AS Spend, COUNT(i.Id) AS Invoices
                FROM sales.Invoice i
                JOIN [platform].[dbo].Shop s ON s.Id = i.ShopId
                WHERE i.CustomerId IN @CustomerIds
                  AND i.ShopId IN @ShopIds
                  AND i.InvoiceDate >= @From AND i.InvoiceDate <= @To
                  AND i.Status NOT IN ('Cancelled')
                GROUP BY i.ShopId, s.TradeName";

            var conn = tenantDb.Database.GetDbConnection();
            var byShop = (await conn.QueryAsync<SpendByShopDto>(sql, new
            {
                CustomerIds = customerIds, ShopIds = shopIds, From = from, To = to
            })).ToList();

            return Result<CustomerInsightsDto>.Success(new CustomerInsightsDto(
                byShop.Sum(x => x.Spend),
                byShop.Sum(x => x.Invoices),
                byShop));
        }, ct);
    }

    public async Task<PagedResult<LinkedShopDto>> ListLinkedShopsAsync(long platformCustomerId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = platformDb.CustomerLinks
            .Where(l => l.PlatformCustomerId == platformCustomerId && l.IsActive);

        var total = await query.CountAsync(ct);
        var links = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        var results = links.Select(l => new LinkedShopDto(l.ShopId, $"Shop {l.ShopId}", false, false, 0m, l.LinkedAtUtc)).ToList();
        return new PagedResult<LinkedShopDto>(results, total, page, pageSize);
    }

    public async Task<IReadOnlyList<string>> GetShopFeaturesAsync(long shopId, CancellationToken ct = default)
    {
        var planCodes = await platformDb.ShopSubscriptions
            .Where(ss => ss.ShopId == shopId && ss.IsActive)
            .SelectMany(ss => ss.Plan.Features)
            .Select(f => f.FeatureCode)
            .ToListAsync(ct);

        var overrides = await platformDb.ShopFeatureOverrides
            .Where(o => o.ShopId == shopId)
            .ToListAsync(ct);

        var enabled  = overrides.Where(o => o.IsEnabled).Select(o => o.FeatureCode);
        var disabled = overrides.Where(o => !o.IsEnabled).Select(o => o.FeatureCode).ToHashSet();

        return planCodes.Union(enabled).Where(c => !disabled.Contains(c)).Distinct().ToList();
    }
}
