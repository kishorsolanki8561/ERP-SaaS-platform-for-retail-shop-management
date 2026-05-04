using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.CustomerPortal.Entities;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.CustomerPortal.Services;

#pragma warning disable CS9107
public sealed class OnlineOrderService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence,
    ITenantContext tenant,
    ILogger<OnlineOrderService> logger)
    : BaseService<TenantDbContext>(db, errorLogger), IOnlineOrderService
{
    public async Task<PagedResult<OnlineOrderSummaryDto>> ListOrdersAsync(int page, int pageSize, OnlineOrderStatus? status, CancellationToken ct = default)
    {
        var query = db.Set<OnlineOrder>().Where(o => o.ShopId == tenant.ShopId);
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new OnlineOrderSummaryDto(o.Id, o.OrderNumber, o.PlatformCustomerId, o.CustomerNameSnapshot, o.Status, o.GrandTotal, o.CreatedAtUtc))
            .ToListAsync(ct);

        return new PagedResult<OnlineOrderSummaryDto>(rows, total, page, pageSize);
    }

    public async Task<Result<OnlineOrderDetailDto?>> GetOrderAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("OnlineOrder.Get", async () =>
        {
            var order = await db.Set<OnlineOrder>()
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order is null) return Result<OnlineOrderDetailDto?>.Success(null);

            var lines = order.Lines.Select(l => new OnlineOrderLineDto(
                l.ProductId, l.ProductNameSnapshot, l.UnitCodeSnapshot, l.QuantityInBilledUnit, l.UnitPriceSnapshot, l.LineTotal)).ToList();

            return Result<OnlineOrderDetailDto?>.Success(new OnlineOrderDetailDto(
                order.Id, order.OrderNumber, order.CustomerNameSnapshot, order.CustomerPhoneSnapshot,
                order.Status, order.RejectionReason, order.DeliveryPreference, order.DeliveryAddressJson,
                order.SubTotal, order.DiscountApplied, order.ShippingCost, order.GrandTotal, lines, order.CreatedAtUtc));
        }, ct);
    }

    public async Task<Result<long>> CreateOrderAsync(CreateOnlineOrderDto dto, long platformCustomerId, CancellationToken ct = default)
    {
        return await ExecuteAsync("OnlineOrder.Create", async () =>
        {
            var orderNumber = await sequence.NextAsync(Constants.SequenceCodes.OnlineOrder, tenant.ShopId, ct);

            var order = new OnlineOrder
            {
                OrderNumber = orderNumber,
                PlatformCustomerId = platformCustomerId,
                TenantCustomerId = dto.TenantCustomerId,
                CustomerNameSnapshot = "Portal Customer",
                CustomerPhoneSnapshot = string.Empty,
                Status = OnlineOrderStatus.Pending,
                DeliveryPreference = dto.DeliveryPreference,
                DeliveryAddressJson = dto.DeliveryAddressJson,
                Notes = dto.Notes,
                ShopId = tenant.ShopId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            var subTotal = 0m;
            foreach (var line in dto.Lines)
            {
                var ol = new OnlineOrderLine
                {
                    ProductId = line.ProductId,
                    ProductNameSnapshot = "Product",
                    UnitCodeSnapshot = "PCS",
                    ConversionFactorSnapshot = 1m,
                    QuantityInBilledUnit = line.Quantity,
                    QuantityInBaseUnit = line.Quantity,
                    UnitPriceSnapshot = 0m,
                    LineTotal = 0m,
                    ShopId = tenant.ShopId,
                    CreatedAtUtc = DateTime.UtcNow,
                };
                order.Lines.Add(ol);
                subTotal += ol.LineTotal;
            }

            order.SubTotal = subTotal;
            order.GrandTotal = subTotal;

            db.Set<OnlineOrder>().Add(order);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Online order {OrderNumber} created for platform customer {PlatformCustomerId}", orderNumber, platformCustomerId);
            return Result<long>.Success(order.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> AcceptOrderAsync(long id, CancellationToken ct = default)
        => await TransitionAsync(id, OnlineOrderStatus.Accepted, ct);

    public async Task<Result<bool>> RejectOrderAsync(long id, string reason, CancellationToken ct = default)
    {
        return await ExecuteAsync("OnlineOrder.Reject", async () =>
        {
            var order = await db.Set<OnlineOrder>().FindAsync([id], ct);
            if (order is null) return Result<bool>.NotFound(Errors.CustomerPortal.OrderNotFound);
            if (order.Status != OnlineOrderStatus.Pending) return Result<bool>.Conflict(Errors.CustomerPortal.OrderNotPending);

            order.Status = OnlineOrderStatus.Rejected;
            order.RejectionReason = reason;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> MarkDispatchedAsync(long id, CancellationToken ct = default)
        => await TransitionAsync(id, OnlineOrderStatus.Dispatched, ct);

    public async Task<Result<bool>> MarkDeliveredAsync(long id, CancellationToken ct = default)
        => await TransitionAsync(id, OnlineOrderStatus.Delivered, ct);

    public async Task<Result<bool>> CancelOrderAsync(long id, CancellationToken ct = default)
        => await TransitionAsync(id, OnlineOrderStatus.Cancelled, ct);

    private async Task<Result<bool>> TransitionAsync(long id, OnlineOrderStatus to, CancellationToken ct)
    {
        return await ExecuteAsync($"OnlineOrder.{to}", async () =>
        {
            var order = await db.Set<OnlineOrder>().FindAsync([id], ct);
            if (order is null) return Result<bool>.NotFound(Errors.CustomerPortal.OrderNotFound);
            if (order.Status == to) return Result<bool>.Success(true);

            order.Status = to;
            if (to == OnlineOrderStatus.Dispatched) order.DispatchedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }
}
