using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Accounting.Entities;
using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Accounting.Services;

public sealed class ChequeService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence,
    ITenantContext tenant,
    ILogger<ChequeService> logger)
    : BaseService<TenantDbContext>(db, errorLogger), IChequeService
{
    public async Task<PagedResult<ChequeListDto>> ListChequesAsync(
        ChequeStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Set<Cheque>()
            .Where(c => !c.IsDeleted && (status == null || c.Status == status));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.ChequeDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new ChequeListDto(
                c.Id, c.Direction, c.ChequeNumber, c.ChequeDate,
                c.Amount, c.DrawerName, c.Status, c.DepositedDate, c.ClearedDate))
            .ToListAsync(ct);

        return new PagedResult<ChequeListDto>(items, total, page, pageSize);
    }

    public async Task<Result<long>> ReceiveChequeAsync(ReceiveChequeDto dto, CancellationToken ct = default)
        => await ExecuteAsync("Cheque.Receive", async () =>
        {
            // Find Cheques-in-Hand and the customer/supplier account from bank account's linked account
            var bankAccount = await db.Set<BankAccount>()
                .FirstOrDefaultAsync(b => b.Id == dto.BankAccountId && !b.IsDeleted, ct);
            if (bankAccount is null)
                return Result<long>.NotFound("Bank account not found");

            var chequesInHandAccount = await db.Set<Account>()
                .FirstOrDefaultAsync(a => a.ShopId == tenant.ShopId && a.Code == "1030" && !a.IsDeleted, ct);
            if (chequesInHandAccount is null)
                return Result<long>.Failure("Cheques-in-Hand account (1030) not found in COA");

            var voucherNumber = await sequence.NextAsync(
                Constants.SequenceCodes.VoucherReceipt, tenant.ShopId, ct);

            // Receipt voucher: Dr Cheques-in-Hand, Cr Bank account linked ledger
            var receiptVoucher = new Voucher
            {
                ShopId = tenant.ShopId,
                VoucherNumber = voucherNumber,
                VoucherDate = dto.ChequeDate,
                VoucherType = VoucherType.Receipt,
                Status = VoucherStatus.Posted,
                IsPosted = true,
                PostedAtUtc = DateTime.UtcNow,
                Narration = $"Cheque received: {dto.ChequeNumber} from {dto.DrawerName}",
                TotalDebit = dto.Amount,
                TotalCredit = dto.Amount,
                SourceDocumentType = "Cheque",
                CreatedAtUtc = DateTime.UtcNow,
            };
            receiptVoucher.Entries.Add(new VoucherEntry
            {
                ShopId = tenant.ShopId,
                AccountId = chequesInHandAccount.Id,
                Type = DebitCredit.Debit,
                Amount = dto.Amount,
                CreatedAtUtc = DateTime.UtcNow,
            });
            receiptVoucher.Entries.Add(new VoucherEntry
            {
                ShopId = tenant.ShopId,
                AccountId = bankAccount.AccountId,
                Type = DebitCredit.Credit,
                Amount = dto.Amount,
                CreatedAtUtc = DateTime.UtcNow,
            });
            db.Set<Voucher>().Add(receiptVoucher);
            await db.SaveChangesAsync(ct);

            var cheque = new Cheque
            {
                ShopId = tenant.ShopId,
                Direction = dto.Direction,
                ChequeNumber = dto.ChequeNumber,
                ChequeDate = dto.ChequeDate,
                ReceivedDate = DateTime.UtcNow,
                Amount = dto.Amount,
                BankAccountId = dto.BankAccountId,
                DrawerName = dto.DrawerName,
                DrawerBankName = dto.DrawerBankName,
                Status = ChequeStatus.Received,
                VoucherIdOnReceive = receiptVoucher.Id,
                RelatedInvoiceId = dto.RelatedInvoiceId,
                RelatedSupplierBillId = dto.RelatedSupplierBillId,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<Cheque>().Add(cheque);
            await db.SaveChangesAsync(ct);

            return Result<long>.Success(cheque.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> DepositChequeAsync(long id, DateTime depositedDate, CancellationToken ct = default)
        => await ExecuteAsync("Cheque.Deposit", async () =>
        {
            var cheque = await db.Set<Cheque>()
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);
            if (cheque is null) return Result<bool>.NotFound("Cheque not found");

            if (cheque.Status != ChequeStatus.Received)
                return Result<bool>.Conflict("Cheque must be in Received status to deposit");

            cheque.Status = ChequeStatus.Deposited;
            cheque.DepositedDate = depositedDate;
            cheque.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> ClearChequeAsync(long id, DateTime clearedDate, CancellationToken ct = default)
        => await ExecuteAsync("Cheque.Clear", async () =>
        {
            var cheque = await db.Set<Cheque>()
                .Include(c => c.BankAccount)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);
            if (cheque is null) return Result<bool>.NotFound("Cheque not found");

            if (cheque.Status != ChequeStatus.Deposited && cheque.Status != ChequeStatus.Received)
                return Result<bool>.Conflict("Cheque must be in Received or Deposited status to clear");

            var chequesInHandAccount = await db.Set<Account>()
                .FirstOrDefaultAsync(a => a.ShopId == tenant.ShopId && a.Code == "1030" && !a.IsDeleted, ct);
            if (chequesInHandAccount is null)
                return Result<bool>.Failure("Cheques-in-Hand account (1030) not found in COA");

            var voucherNumber = await sequence.NextAsync(
                Constants.SequenceCodes.VoucherReceipt, tenant.ShopId, ct);

            // Clear voucher: Dr Bank, Cr Cheques-in-Hand
            var clearVoucher = new Voucher
            {
                ShopId = tenant.ShopId,
                VoucherNumber = voucherNumber,
                VoucherDate = clearedDate,
                VoucherType = VoucherType.Receipt,
                Status = VoucherStatus.Posted,
                IsPosted = true,
                PostedAtUtc = DateTime.UtcNow,
                Narration = $"Cheque cleared: {cheque.ChequeNumber} from {cheque.DrawerName}",
                TotalDebit = cheque.Amount,
                TotalCredit = cheque.Amount,
                SourceDocumentType = "Cheque",
                SourceDocumentId = cheque.Id,
                CreatedAtUtc = DateTime.UtcNow,
            };
            clearVoucher.Entries.Add(new VoucherEntry
            {
                ShopId = tenant.ShopId,
                AccountId = cheque.BankAccount.AccountId,
                Type = DebitCredit.Debit,
                Amount = cheque.Amount,
                CreatedAtUtc = DateTime.UtcNow,
            });
            clearVoucher.Entries.Add(new VoucherEntry
            {
                ShopId = tenant.ShopId,
                AccountId = chequesInHandAccount.Id,
                Type = DebitCredit.Credit,
                Amount = cheque.Amount,
                CreatedAtUtc = DateTime.UtcNow,
            });
            db.Set<Voucher>().Add(clearVoucher);
            await db.SaveChangesAsync(ct);

            cheque.Status = ChequeStatus.Cleared;
            cheque.ClearedDate = clearedDate;
            cheque.VoucherIdOnClear = clearVoucher.Id;
            cheque.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> BounceChequeAsync(long id, BounceChequeDtoRequest dto, CancellationToken ct = default)
        => await ExecuteAsync("Cheque.Bounce", async () =>
        {
            var cheque = await db.Set<Cheque>()
                .Include(c => c.BankAccount)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);
            if (cheque is null) return Result<bool>.NotFound("Cheque not found");

            if (cheque.Status != ChequeStatus.Deposited && cheque.Status != ChequeStatus.Received)
                return Result<bool>.Conflict("Cheque must be in Received or Deposited status to bounce");

            if (!cheque.VoucherIdOnReceive.HasValue)
                return Result<bool>.Failure("No receipt voucher found to reverse");

            var originalVoucher = await db.Set<Voucher>()
                .Include(v => v.Entries)
                .FirstOrDefaultAsync(v => v.Id == cheque.VoucherIdOnReceive, ct);
            if (originalVoucher is null)
                return Result<bool>.Failure("Original receipt voucher not found");

            var voucherNumber = await sequence.NextAsync(
                Constants.SequenceCodes.VoucherJournal, tenant.ShopId, ct);

            // Reversal: mirror original entries with flipped debit/credit
            var reversalVoucher = new Voucher
            {
                ShopId = tenant.ShopId,
                VoucherNumber = voucherNumber,
                VoucherDate = DateTime.UtcNow,
                VoucherType = VoucherType.Journal,
                Status = VoucherStatus.Posted,
                IsPosted = true,
                PostedAtUtc = DateTime.UtcNow,
                Narration = $"Cheque bounce reversal: {cheque.ChequeNumber} — {dto.BounceReasonCode}",
                TotalDebit = cheque.Amount,
                TotalCredit = cheque.Amount,
                SourceDocumentType = "Cheque",
                SourceDocumentId = cheque.Id,
                ReversedByVoucherId = originalVoucher.Id,
                CreatedAtUtc = DateTime.UtcNow,
            };
            foreach (var entry in originalVoucher.Entries)
            {
                reversalVoucher.Entries.Add(new VoucherEntry
                {
                    ShopId = tenant.ShopId,
                    AccountId = entry.AccountId,
                    Type = entry.Type == DebitCredit.Debit ? DebitCredit.Credit : DebitCredit.Debit,
                    Amount = entry.Amount,
                    Narration = "Bounce reversal",
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }

            // Bank charges entry
            if (dto.BounceCharges > 0)
            {
                var bankChargesVoucherNumber = await sequence.NextAsync(
                    Constants.SequenceCodes.VoucherPayment, tenant.ShopId, ct);

                var chargesVoucher = new Voucher
                {
                    ShopId = tenant.ShopId,
                    VoucherNumber = bankChargesVoucherNumber,
                    VoucherDate = DateTime.UtcNow,
                    VoucherType = VoucherType.Payment,
                    Status = VoucherStatus.Posted,
                    IsPosted = true,
                    PostedAtUtc = DateTime.UtcNow,
                    Narration = $"Bank bounce charges for cheque {cheque.ChequeNumber}",
                    TotalDebit = dto.BounceCharges,
                    TotalCredit = dto.BounceCharges,
                    SourceDocumentType = "Cheque",
                    SourceDocumentId = cheque.Id,
                    CreatedAtUtc = DateTime.UtcNow,
                };
                chargesVoucher.Entries.Add(new VoucherEntry
                {
                    ShopId = tenant.ShopId,
                    AccountId = dto.BankChargesAccountId,
                    Type = DebitCredit.Debit,
                    Amount = dto.BounceCharges,
                    CreatedAtUtc = DateTime.UtcNow,
                });
                chargesVoucher.Entries.Add(new VoucherEntry
                {
                    ShopId = tenant.ShopId,
                    AccountId = cheque.BankAccount.AccountId,
                    Type = DebitCredit.Credit,
                    Amount = dto.BounceCharges,
                    CreatedAtUtc = DateTime.UtcNow,
                });
                db.Set<Voucher>().Add(chargesVoucher);
            }

            db.Set<Voucher>().Add(reversalVoucher);
            await db.SaveChangesAsync(ct);

            cheque.Status = ChequeStatus.Bounced;
            cheque.BouncedDate = DateTime.UtcNow;
            cheque.BounceReasonCode = dto.BounceReasonCode;
            cheque.VoucherIdOnBounce = reversalVoucher.Id;
            cheque.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> CancelChequeAsync(long id, CancellationToken ct = default)
        => await ExecuteAsync("Cheque.Cancel", async () =>
        {
            var cheque = await db.Set<Cheque>()
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);
            if (cheque is null) return Result<bool>.NotFound("Cheque not found");

            if (cheque.Status is ChequeStatus.Cleared or ChequeStatus.Bounced)
                return Result<bool>.Conflict("Cannot cancel a cleared or bounced cheque");

            cheque.Status = ChequeStatus.Cancelled;
            cheque.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task MarkStaleDatedAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-3);
        var stale = await db.Set<Cheque>()
            .IgnoreQueryFilters()
            .Where(c => !c.IsDeleted
                && (c.Status == ChequeStatus.Received || c.Status == ChequeStatus.Deposited)
                && c.ChequeDate < cutoff)
            .ToListAsync(ct);

        if (stale.Count == 0) return;

        foreach (var cheque in stale)
        {
            cheque.Status = ChequeStatus.StaleDated;
            cheque.UpdatedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Marked {Count} cheques as stale-dated", stale.Count);
    }
}
