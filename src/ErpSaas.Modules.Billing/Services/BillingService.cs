#pragma warning disable CS9107
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Billing.Entities;
using ErpSaas.Modules.Billing.Enums;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Billing.Services;

public sealed class BillingService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence)
    : BaseService<TenantDbContext>(db, errorLogger), IBillingService
{
    public Task<IReadOnlyList<InvoiceListDto>> ListInvoicesAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken ct = default)
        => db.Set<Invoice>()
            .Where(i => !i.IsDeleted
                && (search == null
                    || i.InvoiceNumber.Contains(search)
                    || i.CustomerNameSnapshot.Contains(search)))
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InvoiceListDto(
                i.Id,
                i.InvoiceNumber,
                i.InvoiceDate,
                i.CustomerNameSnapshot,
                i.Status,
                i.GrandTotal))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<InvoiceListDto>)t.Result, ct,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);

    public Task<InvoiceDetailDto?> GetInvoiceAsync(long id, CancellationToken ct = default)
        => db.Set<Invoice>()
            .Include(i => i.Lines)
            .Where(i => i.Id == id && !i.IsDeleted)
            .Select(i => (InvoiceDetailDto?)new InvoiceDetailDto(
                i.Id,
                i.InvoiceNumber,
                i.InvoiceDate,
                i.CustomerId,
                i.CustomerNameSnapshot,
                i.Status,
                i.SubTotal,
                i.TotalDiscount,
                i.TotalTaxAmount,
                i.GrandTotal,
                i.Lines
                    .Where(l => !l.IsDeleted)
                    .OrderBy(l => l.SortOrder)
                    .Select(l => new InvoiceLineDto(
                        l.Id,
                        l.ProductId,
                        l.ProductNameSnapshot,
                        l.UnitCodeSnapshot,
                        l.QuantityInBilledUnit,
                        l.UnitPrice,
                        l.DiscountPercent,
                        l.TaxableAmount,
                        l.GstRate,
                        l.LineTotal))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

    public async Task<Result<long>> CreateDraftInvoiceAsync(
        CreateInvoiceDto dto,
        CancellationToken ct = default)
        => await ExecuteAsync<long>("Billing.CreateDraftInvoice", async () =>
        {
            var invoiceNumber = await sequence.NextAsync("INVOICE_RETAIL", dto.ShopId, ct);

            var entity = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                InvoiceDate = dto.InvoiceDate,
                CustomerId = dto.CustomerId,
                CustomerNameSnapshot = "Pending",   // populated when CRM module is wired
                Status = InvoiceStatus.Draft,
                SubTotal = 0m,
                TotalDiscount = 0m,
                TotalTaxAmount = 0m,
                RoundOff = 0m,
                GrandTotal = 0m,
                Notes = dto.Notes,
                WarehouseId = dto.WarehouseId,
            };

            db.Set<Invoice>().Add(entity);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> AddLineAsync(
        long invoiceId,
        AddInvoiceLineDto dto,
        CancellationToken ct = default)
        => await ExecuteAsync<bool>("Billing.AddLine", async () =>
        {
            var invoice = await db.Set<Invoice>()
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && !i.IsDeleted, ct);

            if (invoice is null)
                return Result<bool>.NotFound(Errors.Billing.InvoiceNotFound);

            if (invoice.Status != InvoiceStatus.Draft)
                return Result<bool>.Conflict(Errors.Billing.InvoiceNotDraft);

            // TODO: load product + unit snapshots from Inventory when that module is wired.
            // ConversionFactorSnapshot defaults to 1 until Inventory is available.
            var conversionFactor = 1m;
            var qtyInBase = dto.QuantityInBilledUnit * conversionFactor;

            var grossAmount = dto.QuantityInBilledUnit * dto.UnitPrice;
            var discountAmount = grossAmount * dto.DiscountPercent / 100m;
            var taxableAmount = grossAmount - discountAmount;

            // Default GST rate 18 % until product GST rate is loaded from Inventory.
            const decimal defaultGstRate = 18m;
            var cgst = taxableAmount * 0.09m;
            var sgst = taxableAmount * 0.09m;
            var igst = 0m;
            var lineTotal = taxableAmount + cgst + sgst + igst;

            var line = new InvoiceLine
            {
                InvoiceId = invoiceId,
                ProductId = dto.ProductId,
                ProductNameSnapshot = "Pending",        // populated when Inventory is wired
                ProductCodeSnapshot = "Pending",        // populated when Inventory is wired
                ProductUnitId = dto.ProductUnitId,
                UnitCodeSnapshot = "PCS",               // default unit snapshot; replaced when Inventory is wired
                ConversionFactorSnapshot = conversionFactor,
                QuantityInBilledUnit = dto.QuantityInBilledUnit,
                QuantityInBaseUnit = qtyInBase,
                UnitPrice = dto.UnitPrice,
                DiscountPercent = dto.DiscountPercent,
                DiscountAmount = discountAmount,
                TaxableAmount = taxableAmount,
                GstRate = defaultGstRate,
                CgstAmount = cgst,
                SgstAmount = sgst,
                IgstAmount = igst,
                LineTotal = lineTotal,
                SortOrder = invoice.Lines.Count + 1,
                ShopId = invoice.ShopId,
            };

            invoice.Lines.Add(line);
            RecalculateTotals(invoice);
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> FinalizeInvoiceAsync(long id, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Billing.FinalizeInvoice", async () =>
        {
            var invoice = await db.Set<Invoice>()
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, ct);

            if (invoice is null)
                return Result<bool>.NotFound(Errors.Billing.InvoiceNotFound);

            if (invoice.Status != InvoiceStatus.Draft)
                return Result<bool>.Conflict(Errors.Billing.InvoiceNotDraft);

            invoice.Status = InvoiceStatus.Finalized;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> CancelInvoiceAsync(
        long id,
        string reason,
        CancellationToken ct = default)
        => await ExecuteAsync<bool>("Billing.CancelInvoice", async () =>
        {
            var invoice = await db.Set<Invoice>()
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, ct);

            if (invoice is null)
                return Result<bool>.NotFound(Errors.Billing.InvoiceNotFound);

            if (invoice.Status == InvoiceStatus.Cancelled)
                return Result<bool>.Conflict(Errors.Billing.InvoiceAlreadyCancelled);

            invoice.Status = InvoiceStatus.Cancelled;
            invoice.Notes = string.IsNullOrWhiteSpace(invoice.Notes)
                ? $"Cancelled: {reason}"
                : $"{invoice.Notes} | Cancelled: {reason}";

            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void RecalculateTotals(Invoice invoice)
    {
        invoice.SubTotal = invoice.Lines
            .Where(l => !l.IsDeleted)
            .Sum(l => l.QuantityInBilledUnit * l.UnitPrice);

        invoice.TotalDiscount = invoice.Lines
            .Where(l => !l.IsDeleted)
            .Sum(l => l.DiscountAmount);

        invoice.TotalTaxAmount = invoice.Lines
            .Where(l => !l.IsDeleted)
            .Sum(l => l.CgstAmount + l.SgstAmount + l.IgstAmount);

        invoice.GrandTotal = invoice.Lines
            .Where(l => !l.IsDeleted)
            .Sum(l => l.LineTotal) + invoice.RoundOff;
    }
}
