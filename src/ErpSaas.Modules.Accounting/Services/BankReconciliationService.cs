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

public sealed class BankReconciliationService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence,
    ITenantContext tenant,
    ILogger<BankReconciliationService> logger)
    : BaseService<TenantDbContext>(db, errorLogger), IBankReconciliationService
{
    // ── Bank Statements ───────────────────────────────────────────────────────

    public async Task<PagedResult<BankStatementListDto>> ListBankStatementsAsync(
        long? bankAccountId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Set<BankStatement>()
            .Include(s => s.BankAccount)
            .Include(s => s.Lines)
            .Where(s => !s.IsDeleted && (bankAccountId == null || s.BankAccountId == bankAccountId));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.PeriodStart)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new BankStatementListDto(
                s.Id,
                s.BankAccountId,
                s.BankAccount.BankName + " (" + s.BankAccount.AccountNumber + ")",
                s.PeriodStart,
                s.PeriodEnd,
                s.OpeningBalance,
                s.ClosingBalance,
                s.IsCompleted,
                s.Lines.Count,
                s.Lines.Count(l => l.MatchStatus == BankStatementLineStatus.Unmatched)))
            .ToListAsync(ct);

        return new PagedResult<BankStatementListDto>(items, total, page, pageSize);
    }

    public async Task<Result<long>> CreateBankStatementAsync(
        CreateBankStatementDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Accounting.CreateBankStatement", async () =>
        {
            var bankAccount = await db.Set<BankAccount>()
                .FirstOrDefaultAsync(b => b.Id == dto.BankAccountId && !b.IsDeleted, ct);
            if (bankAccount is null)
                return Result<long>.NotFound(Errors.Accounting.BankStatementNotFound);

            var entity = new BankStatement
            {
                BankAccountId = dto.BankAccountId,
                PeriodStart = dto.PeriodStart,
                PeriodEnd = dto.PeriodEnd,
                OpeningBalance = dto.OpeningBalance,
                ClosingBalance = dto.ClosingBalance,
                Notes = dto.Notes,
                IsCompleted = false,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<BankStatement>().Add(entity);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);

    public async Task<BankStatementDetailDto?> GetBankStatementAsync(long id, CancellationToken ct = default)
        => await db.Set<BankStatement>()
            .Include(s => s.BankAccount)
            .Include(s => s.Lines)
            .Where(s => s.Id == id && !s.IsDeleted)
            .Select(s => (BankStatementDetailDto?)new BankStatementDetailDto(
                s.Id,
                s.BankAccountId,
                s.BankAccount.BankName + " (" + s.BankAccount.AccountNumber + ")",
                s.PeriodStart,
                s.PeriodEnd,
                s.OpeningBalance,
                s.ClosingBalance,
                s.IsCompleted,
                s.Notes,
                s.Lines.OrderBy(l => l.TransactionDate).Select(l => new BankStatementLineDto(
                    l.Id, l.TransactionDate, l.Description, l.Reference,
                    l.CreditAmount, l.DebitAmount, l.MatchStatus,
                    l.MatchedVoucherId, l.MatchedAtUtc)).ToList()))
            .FirstOrDefaultAsync(ct);

    public async Task<Result<int>> ImportLinesAsync(
        long statementId, IReadOnlyList<ImportBankStatementLineDto> lines, CancellationToken ct = default)
        => await ExecuteAsync<int>("Accounting.ImportBankStatementLines", async () =>
        {
            var statement = await db.Set<BankStatement>()
                .FirstOrDefaultAsync(s => s.Id == statementId && !s.IsDeleted, ct);
            if (statement is null)
                return Result<int>.NotFound(Errors.Accounting.BankStatementNotFound);
            if (statement.IsCompleted)
                return Result<int>.Conflict(Errors.Accounting.BankStatementAlreadyComplete);

            var entities = lines.Select(l => new BankStatementLine
            {
                BankStatementId = statementId,
                TransactionDate = l.TransactionDate,
                Description = l.Description,
                Reference = l.Reference,
                CreditAmount = l.CreditAmount,
                DebitAmount = l.DebitAmount,
                MatchStatus = BankStatementLineStatus.Unmatched,
            }).ToList();

            db.Set<BankStatementLine>().AddRange(entities);
            await db.SaveChangesAsync(ct);
            return Result<int>.Success(entities.Count);
        }, ct, useTransaction: true);

    // ── Auto Match ────────────────────────────────────────────────────────────

    public async Task<Result<AutoMatchResultDto>> AutoMatchAsync(
        long statementId, CancellationToken ct = default)
        => await ExecuteAsync<AutoMatchResultDto>("Accounting.AutoMatch", async () =>
        {
            var statement = await db.Set<BankStatement>()
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == statementId && !s.IsDeleted, ct);
            if (statement is null)
                return Result<AutoMatchResultDto>.NotFound(Errors.Accounting.BankStatementNotFound);
            if (statement.IsCompleted)
                return Result<AutoMatchResultDto>.Conflict(Errors.Accounting.BankStatementAlreadyComplete);

            var rules = await db.Set<ReconciliationRule>()
                .Where(r => r.IsActive && !r.IsDeleted)
                .ToListAsync(ct);

            var unmatchedLines = statement.Lines
                .Where(l => l.MatchStatus == BankStatementLineStatus.Unmatched)
                .ToList();

            int matchedCount = 0;

            foreach (var line in unmatchedLines)
            {
                var matchingRule = rules.FirstOrDefault(r =>
                    line.Description.Contains(r.PatternContains, StringComparison.OrdinalIgnoreCase));

                if (matchingRule is null) continue;

                var lineAmount = line.CreditAmount > 0 ? line.CreditAmount : line.DebitAmount;
                var window = TimeSpan.FromDays(3);
                var minDate = line.TransactionDate - window;
                var maxDate = line.TransactionDate + window;

                var matchingVoucher = await db.Set<Voucher>()
                    .Where(v => !v.IsDeleted
                        && v.VoucherType == matchingRule.VoucherType
                        && v.VoucherDate >= minDate
                        && v.VoucherDate <= maxDate
                        && v.IsPosted
                        && Math.Abs(v.TotalDebit - lineAmount) < 0.01m)
                    .OrderBy(v => Math.Abs((v.VoucherDate - line.TransactionDate).TotalDays))
                    .FirstOrDefaultAsync(ct);

                if (matchingVoucher is null) continue;

                line.MatchStatus = BankStatementLineStatus.Matched;
                line.MatchedVoucherId = matchingVoucher.Id;
                line.MatchedByUserId = tenant.CurrentUserId;
                line.MatchedAtUtc = DateTime.UtcNow;
                matchedCount++;
            }

            if (matchedCount > 0)
                await db.SaveChangesAsync(ct);

            var stillUnmatched = unmatchedLines.Count - matchedCount;
            return Result<AutoMatchResultDto>.Success(new AutoMatchResultDto(matchedCount, stillUnmatched));
        }, ct, useTransaction: true);

    // ── Manual Operations ─────────────────────────────────────────────────────

    public async Task<Result<bool>> ManualMatchLineAsync(
        long lineId, ManualMatchLineDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Accounting.ManualMatchLine", async () =>
        {
            var line = await db.Set<BankStatementLine>()
                .Include(l => l.BankStatement)
                .FirstOrDefaultAsync(l => l.Id == lineId, ct);
            if (line is null) return Result<bool>.NotFound(Errors.Accounting.BankStatementLineNotFound);
            if (line.BankStatement.IsCompleted)
                return Result<bool>.Conflict(Errors.Accounting.BankStatementAlreadyComplete);
            if (line.MatchStatus == BankStatementLineStatus.Matched)
                return Result<bool>.Conflict(Errors.Accounting.BankStatementLineAlreadyMatched);

            var voucher = await db.Set<Voucher>()
                .FirstOrDefaultAsync(v => v.Id == dto.VoucherId && !v.IsDeleted, ct);
            if (voucher is null) return Result<bool>.NotFound(Errors.Accounting.VoucherNotFound);

            line.MatchStatus = BankStatementLineStatus.Matched;
            line.MatchedVoucherId = dto.VoucherId;
            line.MatchedByUserId = tenant.CurrentUserId;
            line.MatchedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> IgnoreLineAsync(long lineId, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Accounting.IgnoreLine", async () =>
        {
            var line = await db.Set<BankStatementLine>()
                .Include(l => l.BankStatement)
                .FirstOrDefaultAsync(l => l.Id == lineId, ct);
            if (line is null) return Result<bool>.NotFound(Errors.Accounting.BankStatementLineNotFound);
            if (line.BankStatement.IsCompleted)
                return Result<bool>.Conflict(Errors.Accounting.BankStatementAlreadyComplete);
            if (line.MatchStatus == BankStatementLineStatus.Matched)
                return Result<bool>.Conflict(Errors.Accounting.BankStatementLineAlreadyMatched);

            line.MatchStatus = BankStatementLineStatus.Ignored;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<long>> PostAdjustmentAsync(
        PostAdjustmentDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Accounting.PostAdjustment", async () =>
        {
            var line = await db.Set<BankStatementLine>()
                .Include(l => l.BankStatement)
                .FirstOrDefaultAsync(l => l.Id == dto.BankStatementLineId, ct);
            if (line is null) return Result<long>.NotFound(Errors.Accounting.BankStatementLineNotFound);
            if (line.BankStatement.IsCompleted)
                return Result<long>.Conflict(Errors.Accounting.BankStatementAlreadyComplete);
            if (line.MatchStatus == BankStatementLineStatus.Matched)
                return Result<long>.Conflict(Errors.Accounting.BankStatementLineAlreadyMatched);

            var account = await db.Set<Account>()
                .FirstOrDefaultAsync(a => a.Id == dto.AccountId && !a.IsDeleted, ct);
            if (account is null) return Result<long>.NotFound(Errors.Accounting.AccountNotFound);

            var amount = line.CreditAmount > 0 ? line.CreditAmount : line.DebitAmount;
            var isCredit = line.CreditAmount > 0;

            var voucherNumber = await sequence.NextAsync(
                Constants.SequenceCodes.VoucherJournal, tenant.ShopId, ct);

            var bankAccount = await db.Set<BankAccount>()
                .FirstOrDefaultAsync(b => b.Id == line.BankStatement.BankAccountId, ct);

            var voucher = new Voucher
            {
                VoucherNumber = voucherNumber,
                VoucherDate = line.TransactionDate,
                VoucherType = VoucherType.Journal,
                Status = VoucherStatus.Posted,
                Narration = dto.Narration,
                TotalDebit = amount,
                TotalCredit = amount,
                IsPosted = true,
                SourceDocumentType = "BankStatementLine",
                SourceDocumentId = line.Id,
                CreatedAtUtc = DateTime.UtcNow,
            };

            voucher.Entries =
            [
                new VoucherEntry
                {
                    AccountId = isCredit ? dto.AccountId : (bankAccount?.AccountId ?? dto.AccountId),
                    Type = DebitCredit.Debit,
                    Amount = amount,
                    CreatedAtUtc = DateTime.UtcNow,
                },
                new VoucherEntry
                {
                    AccountId = isCredit ? (bankAccount?.AccountId ?? dto.AccountId) : dto.AccountId,
                    Type = DebitCredit.Credit,
                    Amount = amount,
                    CreatedAtUtc = DateTime.UtcNow,
                },
            ];

            db.Set<Voucher>().Add(voucher);
            await db.SaveChangesAsync(ct);

            line.MatchStatus = BankStatementLineStatus.Adjustment;
            line.MatchedVoucherId = voucher.Id;
            line.MatchedByUserId = tenant.CurrentUserId;
            line.MatchedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Result<long>.Success(voucher.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> CompleteReconciliationAsync(
        long statementId, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Accounting.CompleteReconciliation", async () =>
        {
            var statement = await db.Set<BankStatement>()
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == statementId && !s.IsDeleted, ct);
            if (statement is null) return Result<bool>.NotFound(Errors.Accounting.BankStatementNotFound);
            if (statement.IsCompleted)
                return Result<bool>.Conflict(Errors.Accounting.BankStatementAlreadyComplete);

            var hasUnmatched = statement.Lines.Any(l => l.MatchStatus == BankStatementLineStatus.Unmatched);
            if (hasUnmatched)
                return Result<bool>.Conflict(Errors.Accounting.BankStatementLineNotFound);

            statement.IsCompleted = true;
            statement.CompletedAtUtc = DateTime.UtcNow;
            statement.CompletedByUserId = tenant.CurrentUserId;
            statement.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    // ── Reconciliation Rules ──────────────────────────────────────────────────

    public async Task<IReadOnlyList<ReconciliationRuleDto>> ListReconciliationRulesAsync(
        CancellationToken ct = default)
        => await db.Set<ReconciliationRule>()
            .Include(r => r.Account)
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Name)
            .Select(r => new ReconciliationRuleDto(
                r.Id, r.Name, r.PatternContains, r.AccountId,
                r.Account.Name, r.VoucherType, r.IsActive))
            .ToListAsync(ct);

    public async Task<Result<long>> CreateReconciliationRuleAsync(
        CreateReconciliationRuleDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Accounting.CreateReconciliationRule", async () =>
        {
            var account = await db.Set<Account>()
                .FirstOrDefaultAsync(a => a.Id == dto.AccountId && !a.IsDeleted, ct);
            if (account is null) return Result<long>.NotFound(Errors.Accounting.AccountNotFound);

            var rule = new ReconciliationRule
            {
                Name = dto.Name,
                PatternContains = dto.PatternContains,
                AccountId = dto.AccountId,
                VoucherType = dto.VoucherType,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<ReconciliationRule>().Add(rule);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(rule.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> ToggleReconciliationRuleAsync(
        long ruleId, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Accounting.ToggleReconciliationRule", async () =>
        {
            var rule = await db.Set<ReconciliationRule>()
                .FirstOrDefaultAsync(r => r.Id == ruleId && !r.IsDeleted, ct);
            if (rule is null) return Result<bool>.NotFound(Errors.Accounting.ReconciliationRuleNotFound);

            rule.IsActive = !rule.IsActive;
            rule.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
}
