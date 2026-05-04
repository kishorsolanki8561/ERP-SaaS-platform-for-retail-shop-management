using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.SalesReturns.Entities;
using ErpSaas.Modules.SalesReturns.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.SalesReturns.Services;

public sealed class SalesReturnsService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence,
    ITenantContext tenant,
    IAutoVoucherService autoVoucher,
    IWalletRefundOrchestrator? refundOrchestrator = null)
    : BaseService<TenantDbContext>(db, errorLogger), ISalesReturnsService
{
    public async Task<Result<long>> CreateSalesReturnAsync(CreateSalesReturnDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("SalesReturns.CreateSalesReturn", async () =>
        {
            var returnNumber = await sequence.NextAsync(Constants.SequenceCodes.SalesReturn, tenant.ShopId, ct);

            var sr = new SalesReturn
            {
                ShopId = tenant.ShopId,
                ReturnNumber = returnNumber,
                InvoiceId = dto.InvoiceId,
                InvoiceNumberSnapshot = $"INV-{dto.InvoiceId}",
                CustomerId = 0,
                CustomerNameSnapshot = "Customer",
                ReturnDate = dto.ReturnDate,
                Status = SalesReturnStatus.Draft,
                RefundMethod = dto.RefundMethod,
                Reason = dto.Reason,
                CreatedAtUtc = DateTime.UtcNow,
            };

            decimal total = 0;
            foreach (var line in dto.Lines)
            {
                var lineEntity = new SalesReturnLine
                {
                    ShopId = tenant.ShopId,
                    InvoiceLineId = line.InvoiceLineId,
                    ProductId = line.ProductId,
                    ProductNameSnapshot = "Product",
                    ProductCodeSnapshot = line.ProductId.ToString(),
                    ProductUnitId = line.ProductUnitId,
                    UnitCodeSnapshot = "PCS",
                    ConversionFactorSnapshot = 1,
                    QuantityInBilledUnit = line.Quantity,
                    QuantityInBaseUnit = line.Quantity,
                    UnitPrice = 0,
                    LineTotal = 0,
                    CreatedAtUtc = DateTime.UtcNow,
                };
                sr.Lines.Add(lineEntity);
            }
            sr.TotalRefundAmount = total;

            _db.Set<SalesReturn>().Add(sr);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(sr.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> ApproveSalesReturnAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("SalesReturns.ApproveSalesReturn", async () =>
        {
            var sr = await _db.Set<SalesReturn>().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (sr is null) return Result<bool>.NotFound(Errors.SalesReturns.SalesReturnNotFound);
            if (sr.Status != SalesReturnStatus.Draft) return Result<bool>.Conflict(Errors.SalesReturns.SalesReturnNotDraft);

            sr.Status = SalesReturnStatus.Approved;
            sr.UpdatedAtUtc = DateTime.UtcNow;

            // Route refund: wallet credit for WalletCredit method, cash recorded otherwise.
            var toWallet = sr.RefundMethod == RefundMethod.WalletCredit ? sr.TotalRefundAmount : 0m;
            var toCash = sr.RefundMethod == RefundMethod.Cash ? sr.TotalRefundAmount : 0m;

            if (refundOrchestrator is not null && (toWallet > 0 || toCash > 0))
            {
                var refundResult = await refundOrchestrator.ProcessRefundAsync(
                    sr.Id, sr.CustomerId, sr.CustomerNameSnapshot,
                    customerPhone: null,
                    sr.ReturnNumber, toWallet, toCash, ct);

                if (!refundResult.IsSuccess)
                    return Result<bool>.Failure(refundResult.Errors.FirstOrDefault() ?? "WALLET_ERR");
            }

            sr.RefundedToWallet = toWallet > 0 ? toWallet : null;
            sr.RefundedToCash = toCash > 0 ? toCash : null;

            await _db.SaveChangesAsync(ct);
            await autoVoucher.PostSalesReturnVoucherAsync(tenant.ShopId, sr.Id, sr.ReturnNumber, sr.TotalRefundAmount, ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> CancelSalesReturnAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("SalesReturns.CancelSalesReturn", async () =>
        {
            var sr = await _db.Set<SalesReturn>().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (sr is null) return Result<bool>.NotFound(Errors.SalesReturns.SalesReturnNotFound);
            if (sr.Status == SalesReturnStatus.Cancelled) return Result<bool>.Conflict(Errors.SalesReturns.SalesReturnCancelled);

            sr.Status = SalesReturnStatus.Cancelled;
            sr.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<long>> IssueCreditNoteAsync(IssueCreditNoteDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("SalesReturns.IssueCreditNote", async () =>
        {
            var cnNumber = await sequence.NextAsync(Constants.SequenceCodes.CreditNote, tenant.ShopId, ct);

            var cn = new CreditNote
            {
                ShopId = tenant.ShopId,
                CreditNoteNumber = cnNumber,
                CustomerId = dto.CustomerId,
                CustomerNameSnapshot = "Customer",
                IssueDate = DateTime.UtcNow,
                ExpiryDate = dto.ExpiryDate,
                Status = CreditNoteStatus.Issued,
                Amount = dto.Amount,
                UsedAmount = 0,
                RemainingAmount = dto.Amount,
                Notes = dto.Notes,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<CreditNote>().Add(cn);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(cn.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> ApplyCreditNoteAsync(ApplyCreditNoteDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("SalesReturns.ApplyCreditNote", async () =>
        {
            var cn = await _db.Set<CreditNote>().FirstOrDefaultAsync(x => x.Id == dto.CreditNoteId, ct);
            if (cn is null) return Result<bool>.NotFound(Errors.SalesReturns.CreditNoteNotFound);
            if (cn.Status != CreditNoteStatus.Issued) return Result<bool>.Conflict(Errors.SalesReturns.CreditNoteNotIssued);
            if (cn.ExpiryDate.HasValue && cn.ExpiryDate.Value < DateTime.UtcNow) return Result<bool>.Conflict(Errors.SalesReturns.CreditNoteExpired);
            if (dto.AmountToApply > cn.RemainingAmount) return Result<bool>.Conflict(Errors.SalesReturns.CreditNoteInsufficient);

            cn.UsedAmount += dto.AmountToApply;
            cn.RemainingAmount -= dto.AmountToApply;
            if (cn.RemainingAmount <= 0) cn.Status = CreditNoteStatus.Applied;
            cn.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> CancelCreditNoteAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("SalesReturns.CancelCreditNote", async () =>
        {
            var cn = await _db.Set<CreditNote>().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (cn is null) return Result<bool>.NotFound(Errors.SalesReturns.CreditNoteNotFound);

            cn.Status = CreditNoteStatus.Cancelled;
            cn.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }
}
