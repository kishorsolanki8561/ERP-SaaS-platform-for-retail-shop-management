using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Marketplace.Entities;
using ErpSaas.Modules.Marketplace.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Marketplace.Services;

public sealed class MarketplaceOrderService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), IMarketplaceOrderService
{
    public async Task<IReadOnlyList<MarketplaceOrderDto>> ListAsync(MarketplaceOrderListRequest request, CancellationToken ct = default)
    {
        var query = _db.Set<MarketplaceOrder>().AsQueryable();

        if (request.AccountId.HasValue)
            query = query.Where(o => o.MarketplaceAccountId == request.AccountId.Value);
        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);
        if (request.From.HasValue)
            query = query.Where(o => o.OrderDate >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(o => o.OrderDate <= request.To.Value);

        return await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => Map(o))
            .ToListAsync(ct);
    }

    public async Task<Result<long>> IngestAsync(IngestOrderDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Marketplace.Order.Ingest", async () =>
        {
            var duplicate = await _db.Set<MarketplaceOrder>()
                .AnyAsync(o => o.MarketplaceAccountId == dto.MarketplaceAccountId
                               && o.MarketplaceOrderId == dto.MarketplaceOrderId, ct);
            if (duplicate) return Result<long>.Conflict(Errors.Marketplace.OrderAlreadyIngested);

            var entity = new MarketplaceOrder
            {
                ShopId = tenant.ShopId,
                MarketplaceAccountId = dto.MarketplaceAccountId,
                MarketplaceOrderId = dto.MarketplaceOrderId,
                OrderDate = dto.OrderDate,
                CustomerName = dto.CustomerName,
                CustomerPhone = dto.CustomerPhone,
                ShippingAddressJson = dto.ShippingAddressJson,
                OrderTotal = dto.OrderTotal,
                Status = MarketplaceOrderStatus.New,
                RawPayloadJson = dto.RawPayloadJson,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<MarketplaceOrder>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<long>> ConvertToInvoiceAsync(long orderId, CancellationToken ct = default)
    {
        return await ExecuteAsync("Marketplace.Order.ConvertToInvoice", async () =>
        {
            var order = await _db.Set<MarketplaceOrder>().FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order is null) return Result<long>.NotFound(Errors.Marketplace.OrderNotFound);
            if (order.Status == MarketplaceOrderStatus.Converted)
                return Result<long>.Conflict(Errors.Marketplace.OrderAlreadyConverted);
            if (order.Status == MarketplaceOrderStatus.Cancelled)
                return Result<long>.Conflict(Errors.Marketplace.OrderCancelled);

            // Stub: real impl delegates to IBillingService.CreateInvoiceAsync
            // and then updates order.Status = Converted + order.GeneratedInvoiceId
            order.Status = MarketplaceOrderStatus.Converted;
            order.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(order.Id);
        }, ct, useTransaction: true);
    }

    private static MarketplaceOrderDto Map(MarketplaceOrder o) => new(
        o.Id, o.MarketplaceAccountId, o.MarketplaceOrderId,
        o.OrderDate, o.CustomerName, o.CustomerPhone,
        o.OrderTotal, o.Status, o.GeneratedInvoiceId);
}
