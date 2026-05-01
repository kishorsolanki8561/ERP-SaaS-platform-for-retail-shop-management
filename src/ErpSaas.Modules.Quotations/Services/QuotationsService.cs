using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.Quotations.Entities;
using ErpSaas.Modules.Quotations.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
namespace ErpSaas.Modules.Quotations.Services;

public sealed class QuotationsService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), IQuotationsService
{
    // ── Quotations ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<QuotationSummaryDto>> ListQuotationsAsync(CancellationToken ct = default)
        => await _db.Set<Quotation>()
            .Where(q => !q.IsDeleted)
            .Select(q => new QuotationSummaryDto(
                q.Id, q.QuotationNumber, q.CustomerId, q.CustomerNameSnapshot,
                q.Status, q.QuotationDate, q.ValidUntil, q.GrandTotal))
            .ToListAsync(ct);

    public async Task<Result<long>> CreateQuotationAsync(CreateQuotationDto dto, CancellationToken ct = default)
        => await ExecuteAsync("Quotations.CreateQuotation", async () =>
        {
            var number = await sequence.NextAsync(Constants.SequenceCodes.Quotation, tenant.ShopId, ct);
            var quotation = new Quotation
            {
                ShopId = tenant.ShopId,
                QuotationNumber = number,
                CustomerId = dto.CustomerId,
                CustomerNameSnapshot = dto.CustomerNameSnapshot,
                Status = QuotationStatus.Draft,
                QuotationDate = DateTime.Today,
                ValidUntil = dto.ValidUntil,
                Notes = dto.Notes,
                CreatedAtUtc = DateTime.UtcNow,
            };

            foreach (var l in dto.Lines)
            {
                var qty = Math.Round(l.QuantityInBilledUnit * l.ConversionFactor, 4);
                var gross = Math.Round(l.QuantityInBilledUnit * l.UnitPrice, 2);
                var taxable = Math.Round(gross - l.DiscountAmount, 2);
                var tax = Math.Round(taxable * l.GstRate / 100, 2);
                quotation.Lines.Add(new QuotationLine
                {
                    ProductId = l.ProductId,
                    ProductNameSnapshot = l.ProductNameSnapshot,
                    ProductUnitId = l.ProductUnitId,
                    UnitCodeSnapshot = l.UnitCodeSnapshot,
                    ConversionFactorSnapshot = l.ConversionFactor,
                    QuantityInBilledUnit = l.QuantityInBilledUnit,
                    QuantityInBaseUnit = qty,
                    UnitPrice = l.UnitPrice,
                    DiscountAmount = l.DiscountAmount,
                    TaxableAmount = taxable,
                    GstRate = l.GstRate,
                    TaxAmount = tax,
                    LineTotal = taxable + tax,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }

            quotation.SubTotal = quotation.Lines.Sum(l => l.QuantityInBilledUnit * l.UnitPrice);
            quotation.TotalDiscount = quotation.Lines.Sum(l => l.DiscountAmount);
            quotation.TotalTax = quotation.Lines.Sum(l => l.TaxAmount);
            quotation.GrandTotal = quotation.Lines.Sum(l => l.LineTotal);

            _db.Set<Quotation>().Add(quotation);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(quotation.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> SendQuotationAsync(long id, CancellationToken ct = default)
        => await ExecuteAsync("Quotations.SendQuotation", async () =>
        {
            var q = await _db.Set<Quotation>().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (q is null) return Result<bool>.NotFound(Errors.Quotations.QuotationNotFound);
            if (q.Status != QuotationStatus.Draft) return Result<bool>.Conflict(Errors.Quotations.QuotationNotInDraft);
            q.Status = QuotationStatus.Sent;
            q.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<long>> ConvertQuotationToSalesOrderAsync(long quotationId, CancellationToken ct = default)
        => await ExecuteAsync("Quotations.ConvertQuotation", async () =>
        {
            var q = await _db.Set<Quotation>()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == quotationId, ct);
            if (q is null) return Result<long>.NotFound(Errors.Quotations.QuotationNotFound);
            if (q.Status is QuotationStatus.Expired or QuotationStatus.Rejected)
                return Result<long>.Conflict(Errors.Quotations.QuotationExpired);

            var soNumber = await sequence.NextAsync(Constants.SequenceCodes.SalesOrder, tenant.ShopId, ct);
            var so = new SalesOrder
            {
                ShopId = tenant.ShopId,
                SoNumber = soNumber,
                QuotationId = quotationId,
                CustomerId = q.CustomerId,
                CustomerNameSnapshot = q.CustomerNameSnapshot,
                Status = SalesOrderStatus.Confirmed,
                OrderDate = DateTime.Today,
                SubTotal = q.SubTotal,
                TotalDiscount = q.TotalDiscount,
                TotalTax = q.TotalTax,
                GrandTotal = q.GrandTotal,
                Notes = q.Notes,
                BranchId = q.BranchId,
                CreatedAtUtc = DateTime.UtcNow,
            };
            foreach (var l in q.Lines)
            {
                so.Lines.Add(new SalesOrderLine
                {
                    ProductId = l.ProductId,
                    ProductNameSnapshot = l.ProductNameSnapshot,
                    ProductUnitId = l.ProductUnitId,
                    UnitCodeSnapshot = l.UnitCodeSnapshot,
                    ConversionFactorSnapshot = l.ConversionFactorSnapshot,
                    QuantityInBilledUnit = l.QuantityInBilledUnit,
                    QuantityInBaseUnit = l.QuantityInBaseUnit,
                    UnitPrice = l.UnitPrice,
                    DiscountAmount = l.DiscountAmount,
                    TaxableAmount = l.TaxableAmount,
                    GstRate = l.GstRate,
                    TaxAmount = l.TaxAmount,
                    LineTotal = l.LineTotal,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }

            q.Status = QuotationStatus.Converted;
            q.UpdatedAtUtc = DateTime.UtcNow;

            _db.Set<SalesOrder>().Add(so);
            await _db.SaveChangesAsync(ct);
            q.ConvertedToSalesOrderId = so.Id;
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(so.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> RejectQuotationAsync(long id, CancellationToken ct = default)
        => await ExecuteAsync("Quotations.RejectQuotation", async () =>
        {
            var q = await _db.Set<Quotation>().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (q is null) return Result<bool>.NotFound(Errors.Quotations.QuotationNotFound);
            q.Status = QuotationStatus.Rejected;
            q.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    // ── Sales Orders ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SalesOrderSummaryDto>> ListSalesOrdersAsync(CancellationToken ct = default)
        => await _db.Set<SalesOrder>()
            .Where(s => !s.IsDeleted)
            .Select(s => new SalesOrderSummaryDto(
                s.Id, s.SoNumber, s.CustomerId, s.CustomerNameSnapshot,
                s.Status, s.OrderDate, s.GrandTotal))
            .ToListAsync(ct);

    public async Task<Result<long>> CreateSalesOrderAsync(CreateSalesOrderDto dto, CancellationToken ct = default)
        => await ExecuteAsync("Quotations.CreateSalesOrder", async () =>
        {
            var soNumber = await sequence.NextAsync(Constants.SequenceCodes.SalesOrder, tenant.ShopId, ct);
            var so = new SalesOrder
            {
                ShopId = tenant.ShopId,
                SoNumber = soNumber,
                QuotationId = dto.QuotationId,
                CustomerId = dto.CustomerId,
                CustomerNameSnapshot = dto.CustomerNameSnapshot,
                Status = SalesOrderStatus.Confirmed,
                OrderDate = DateTime.Today,
                ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
                ShippingAddress = dto.ShippingAddress,
                Notes = dto.Notes,
                CreatedAtUtc = DateTime.UtcNow,
            };
            foreach (var l in dto.Lines)
            {
                var qty = Math.Round(l.QuantityInBilledUnit * l.ConversionFactor, 4);
                var gross = Math.Round(l.QuantityInBilledUnit * l.UnitPrice, 2);
                var taxable = Math.Round(gross - l.DiscountAmount, 2);
                var tax = Math.Round(taxable * l.GstRate / 100, 2);
                so.Lines.Add(new SalesOrderLine
                {
                    ProductId = l.ProductId,
                    ProductNameSnapshot = l.ProductNameSnapshot,
                    ProductUnitId = l.ProductUnitId,
                    UnitCodeSnapshot = l.UnitCodeSnapshot,
                    ConversionFactorSnapshot = l.ConversionFactor,
                    QuantityInBilledUnit = l.QuantityInBilledUnit,
                    QuantityInBaseUnit = qty,
                    UnitPrice = l.UnitPrice,
                    DiscountAmount = l.DiscountAmount,
                    TaxableAmount = taxable,
                    GstRate = l.GstRate,
                    TaxAmount = tax,
                    LineTotal = taxable + tax,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }
            so.SubTotal = so.Lines.Sum(l => l.QuantityInBilledUnit * l.UnitPrice);
            so.TotalDiscount = so.Lines.Sum(l => l.DiscountAmount);
            so.TotalTax = so.Lines.Sum(l => l.TaxAmount);
            so.GrandTotal = so.Lines.Sum(l => l.LineTotal);

            _db.Set<SalesOrder>().Add(so);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(so.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> CancelSalesOrderAsync(long id, CancellationToken ct = default)
        => await ExecuteAsync("Quotations.CancelSalesOrder", async () =>
        {
            var so = await _db.Set<SalesOrder>().FirstOrDefaultAsync(s => s.Id == id, ct);
            if (so is null) return Result<bool>.NotFound(Errors.Quotations.SalesOrderNotFound);
            if (so.Status == SalesOrderStatus.Cancelled)
                return Result<bool>.Conflict(Errors.Quotations.SalesOrderAlreadyCancelled);
            so.Status = SalesOrderStatus.Cancelled;
            so.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    // ── Delivery Challans ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<DeliveryChallanSummaryDto>> ListDeliveryChallansAsync(CancellationToken ct = default)
        => await _db.Set<DeliveryChallan>()
            .Where(d => !d.IsDeleted)
            .Select(d => new DeliveryChallanSummaryDto(
                d.Id, d.DcNumber, d.SalesOrderId, d.Status, d.ChallanDate))
            .ToListAsync(ct);

    public async Task<Result<long>> CreateDeliveryChallanAsync(CreateDeliveryChallanDto dto, CancellationToken ct = default)
        => await ExecuteAsync("Quotations.CreateDeliveryChallan", async () =>
        {
            var so = await _db.Set<SalesOrder>()
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == dto.SalesOrderId, ct);
            if (so is null) return Result<long>.NotFound(Errors.Quotations.SalesOrderNotFound);

            var dcNumber = await sequence.NextAsync(Constants.SequenceCodes.DeliveryChallan, tenant.ShopId, ct);
            var dc = new DeliveryChallan
            {
                ShopId = tenant.ShopId,
                DcNumber = dcNumber,
                SalesOrderId = dto.SalesOrderId,
                Status = DeliveryChallanStatus.Draft,
                ChallanDate = dto.ChallanDate,
                DeliveryAddress = dto.DeliveryAddress,
                TransporterName = dto.TransporterName,
                VehicleNumber = dto.VehicleNumber,
                Notes = dto.Notes,
                BranchId = so.BranchId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            foreach (var l in so.Lines)
            {
                dc.Lines.Add(new DeliveryChallanLine
                {
                    ProductId = l.ProductId,
                    ProductNameSnapshot = l.ProductNameSnapshot,
                    ProductUnitId = l.ProductUnitId,
                    UnitCodeSnapshot = l.UnitCodeSnapshot,
                    ConversionFactorSnapshot = l.ConversionFactorSnapshot,
                    QuantityInBilledUnit = l.QuantityInBilledUnit,
                    QuantityInBaseUnit = l.QuantityInBaseUnit,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }

            _db.Set<DeliveryChallan>().Add(dc);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(dc.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> DispatchDeliveryChallanAsync(long id, CancellationToken ct = default)
        => await ExecuteAsync("Quotations.DispatchDeliveryChallan", async () =>
        {
            var dc = await _db.Set<DeliveryChallan>().FirstOrDefaultAsync(d => d.Id == id, ct);
            if (dc is null) return Result<bool>.NotFound(Errors.Quotations.DeliveryChallanNotFound);
            if (dc.Status != DeliveryChallanStatus.Draft)
                return Result<bool>.Conflict(Errors.Quotations.DeliveryChallanAlreadyDispatched);
            dc.Status = DeliveryChallanStatus.Dispatched;
            dc.DispatchedDate = DateTime.UtcNow;
            dc.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> MarkDeliveryChallanDeliveredAsync(long id, CancellationToken ct = default)
        => await ExecuteAsync("Quotations.MarkDelivered", async () =>
        {
            var dc = await _db.Set<DeliveryChallan>().FirstOrDefaultAsync(d => d.Id == id, ct);
            if (dc is null) return Result<bool>.NotFound(Errors.Quotations.DeliveryChallanNotFound);
            dc.Status = DeliveryChallanStatus.Delivered;
            dc.DeliveredDate = DateTime.UtcNow;
            dc.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
}
