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

public sealed class PettyCashService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence,
    ITenantContext tenant,
    ILogger<PettyCashService> logger)
    : BaseService<TenantDbContext>(db, errorLogger), IPettyCashService
{
    // COA code seeded in AccountingTenantSeeder
    private const string PettyCashCode = "1110";

    public async Task<Result<long>> TopUpAsync(PettyCashTopUpDto dto, CancellationToken ct = default)
        => await ExecuteAsync("PettyCash.TopUp", async () =>
        {
            var pettyCash = await GetPettyCashAccountAsync(ct);
            if (pettyCash is null)
                return Result<long>.Failure("Petty Cash account (1110) not found in COA");

            var bankAccount = await db.Set<BankAccount>()
                .FirstOrDefaultAsync(b => b.Id == dto.FromBankAccountId && !b.IsDeleted, ct);
            if (bankAccount is null)
                return Result<long>.NotFound("Bank account not found");

            var voucherNumber = await sequence.NextAsync(
                Constants.SequenceCodes.VoucherContra, tenant.ShopId, ct);

            var voucher = new Voucher
            {
                ShopId = tenant.ShopId,
                VoucherNumber = voucherNumber,
                VoucherDate = DateTime.UtcNow,
                VoucherType = VoucherType.Contra,
                Status = VoucherStatus.Posted,
                IsPosted = true,
                PostedAtUtc = DateTime.UtcNow,
                Narration = dto.Narration ?? "Petty cash top-up",
                TotalDebit = dto.Amount,
                TotalCredit = dto.Amount,
                SourceDocumentType = "PettyCashTopUp",
                CreatedAtUtc = DateTime.UtcNow,
            };
            voucher.Entries.Add(new VoucherEntry
            {
                ShopId = tenant.ShopId,
                AccountId = pettyCash.Id,
                Type = DebitCredit.Debit,
                Amount = dto.Amount,
                CreatedAtUtc = DateTime.UtcNow,
            });
            voucher.Entries.Add(new VoucherEntry
            {
                ShopId = tenant.ShopId,
                AccountId = bankAccount.AccountId,
                Type = DebitCredit.Credit,
                Amount = dto.Amount,
                CreatedAtUtc = DateTime.UtcNow,
            });
            db.Set<Voucher>().Add(voucher);
            await db.SaveChangesAsync(ct);

            return Result<long>.Success(voucher.Id);
        }, ct, useTransaction: true);

    public async Task<Result<long>> RecordExpenseAsync(PettyCashExpenseDto dto, CancellationToken ct = default)
        => await ExecuteAsync("PettyCash.Expense", async () =>
        {
            var pettyCash = await GetPettyCashAccountAsync(ct);
            if (pettyCash is null)
                return Result<long>.Failure("Petty Cash account (1110) not found in COA");

            var expenseAccount = await db.Set<Account>()
                .FirstOrDefaultAsync(a => a.Id == dto.ExpenseAccountId && !a.IsDeleted, ct);
            if (expenseAccount is null)
                return Result<long>.NotFound("Expense account not found");

            var voucherNumber = await sequence.NextAsync(
                Constants.SequenceCodes.VoucherPayment, tenant.ShopId, ct);

            var voucher = new Voucher
            {
                ShopId = tenant.ShopId,
                VoucherNumber = voucherNumber,
                VoucherDate = DateTime.UtcNow,
                VoucherType = VoucherType.Payment,
                Status = VoucherStatus.Posted,
                IsPosted = true,
                PostedAtUtc = DateTime.UtcNow,
                Narration = dto.Description,
                TotalDebit = dto.Amount,
                TotalCredit = dto.Amount,
                SourceDocumentType = "PettyCashExpense",
                CreatedAtUtc = DateTime.UtcNow,
            };
            voucher.Entries.Add(new VoucherEntry
            {
                ShopId = tenant.ShopId,
                AccountId = expenseAccount.Id,
                Type = DebitCredit.Debit,
                Amount = dto.Amount,
                CreatedAtUtc = DateTime.UtcNow,
            });
            voucher.Entries.Add(new VoucherEntry
            {
                ShopId = tenant.ShopId,
                AccountId = pettyCash.Id,
                Type = DebitCredit.Credit,
                Amount = dto.Amount,
                CreatedAtUtc = DateTime.UtcNow,
            });
            db.Set<Voucher>().Add(voucher);
            await db.SaveChangesAsync(ct);

            return Result<long>.Success(voucher.Id);
        }, ct, useTransaction: true);

    public async Task<Result<long>> ClosePeriodAsync(PettyCashClosureDto dto, CancellationToken ct = default)
        => await ExecuteAsync("PettyCash.Close", async () =>
        {
            var pettyCash = await GetPettyCashAccountAsync(ct);
            if (pettyCash is null)
                return Result<long>.Failure("Petty Cash account (1110) not found in COA");

            // Running balance = sum of debit entries minus credit entries on petty cash account
            // Materialized to client to stay compatible with both SQL Server and SQLite
            var entries = await db.Set<VoucherEntry>()
                .Where(ve => ve.AccountId == pettyCash.Id && !ve.IsDeleted)
                .Select(ve => new { ve.Type, ve.Amount })
                .ToListAsync(ct);
            var expectedBalance = entries.Sum(ve =>
                ve.Type == DebitCredit.Debit ? ve.Amount : -ve.Amount);

            var variance = dto.CountedBalance - expectedBalance;

            long? varianceVoucherId = null;
            if (variance != 0)
            {
                var overshortAccount = await db.Set<Account>()
                    .FirstOrDefaultAsync(a => a.ShopId == tenant.ShopId
                        && a.Code == (variance > 0 ? "4810" : "5810") && !a.IsDeleted, ct);

                if (overshortAccount is not null)
                {
                    var voucherNumber = await sequence.NextAsync(
                        Constants.SequenceCodes.VoucherJournal, tenant.ShopId, ct);

                    var varianceVoucher = new Voucher
                    {
                        ShopId = tenant.ShopId,
                        VoucherNumber = voucherNumber,
                        VoucherDate = dto.ClosureDate,
                        VoucherType = VoucherType.Journal,
                        Status = VoucherStatus.Posted,
                        IsPosted = true,
                        PostedAtUtc = DateTime.UtcNow,
                        Narration = $"Petty cash variance on closure — {dto.Narration}",
                        TotalDebit = Math.Abs(variance),
                        TotalCredit = Math.Abs(variance),
                        SourceDocumentType = "PettyCashClosure",
                        CreatedAtUtc = DateTime.UtcNow,
                    };

                    // Surplus: Dr PettyCash Cr Overage; Shortage: Dr Shortage Cr PettyCash
                    if (variance > 0)
                    {
                        varianceVoucher.Entries.Add(new VoucherEntry { ShopId = tenant.ShopId, AccountId = pettyCash.Id, Type = DebitCredit.Debit, Amount = variance, CreatedAtUtc = DateTime.UtcNow });
                        varianceVoucher.Entries.Add(new VoucherEntry { ShopId = tenant.ShopId, AccountId = overshortAccount.Id, Type = DebitCredit.Credit, Amount = variance, CreatedAtUtc = DateTime.UtcNow });
                    }
                    else
                    {
                        varianceVoucher.Entries.Add(new VoucherEntry { ShopId = tenant.ShopId, AccountId = overshortAccount.Id, Type = DebitCredit.Debit, Amount = Math.Abs(variance), CreatedAtUtc = DateTime.UtcNow });
                        varianceVoucher.Entries.Add(new VoucherEntry { ShopId = tenant.ShopId, AccountId = pettyCash.Id, Type = DebitCredit.Credit, Amount = Math.Abs(variance), CreatedAtUtc = DateTime.UtcNow });
                    }

                    db.Set<Voucher>().Add(varianceVoucher);
                    await db.SaveChangesAsync(ct);
                    varianceVoucherId = varianceVoucher.Id;
                }
            }

            var closure = new PettyCashClosure
            {
                ShopId = tenant.ShopId,
                ClosureDate = dto.ClosureDate,
                ExpectedBalance = expectedBalance,
                CountedBalance = dto.CountedBalance,
                Variance = variance,
                Narration = dto.Narration,
                VarianceVoucherId = varianceVoucherId,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<PettyCashClosure>().Add(closure);
            await db.SaveChangesAsync(ct);

            return Result<long>.Success(closure.Id);
        }, ct, useTransaction: true);

    public async Task<IReadOnlyList<PettyCashClosureListDto>> ListClosuresAsync(CancellationToken ct = default)
        => await db.Set<PettyCashClosure>()
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.ClosureDate)
            .Select(c => new PettyCashClosureListDto(
                c.Id, c.ClosureDate, c.ExpectedBalance, c.CountedBalance, c.Variance))
            .ToListAsync(ct);

    private Task<Account?> GetPettyCashAccountAsync(CancellationToken ct)
        => db.Set<Account>()
            .FirstOrDefaultAsync(a => a.ShopId == tenant.ShopId && a.Code == PettyCashCode && !a.IsDeleted, ct);
}
