using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Accounting.Entities;
using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Accounting.Services;

public sealed class AccountingService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), IAccountingService, IAutoVoucherService
{
    // ── Account Groups ────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<AccountGroupDto>> ListAccountGroupsAsync(CancellationToken ct = default)
        => await _db.Set<AccountGroup>()
            .Where(g => !g.IsDeleted)
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Name)
            .Select(g => new AccountGroupDto(g.Id, g.Name, g.Code, g.ParentId, g.Nature, g.IsSystem, g.SortOrder))
            .ToListAsync(ct);

    // ── Accounts ──────────────────────────────────────────────────────────────

    public async Task<PagedResult<AccountListDto>> ListAccountsAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = _db.Set<Account>()
            .Include(a => a.AccountGroup)
            .Where(a => !a.IsDeleted
                && (search == null || a.Name.Contains(search) || a.Code.Contains(search)));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(a => a.Code)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AccountListDto(
                a.Id, a.Name, a.Code, a.AccountGroupId, a.AccountGroup.Name,
                a.AccountGroup.Nature, a.IsSystem, a.IsActive,
                a.OpeningBalance, a.OpeningBalanceType))
            .ToListAsync(ct);

        return new PagedResult<AccountListDto>(items, total, page, pageSize);
    }

    public async Task<AccountListDto?> GetAccountAsync(long id, CancellationToken ct = default)
        => await _db.Set<Account>()
            .Include(a => a.AccountGroup)
            .Where(a => a.Id == id && !a.IsDeleted)
            .Select(a => (AccountListDto?)new AccountListDto(
                a.Id, a.Name, a.Code, a.AccountGroupId, a.AccountGroup.Name,
                a.AccountGroup.Nature, a.IsSystem, a.IsActive,
                a.OpeningBalance, a.OpeningBalanceType))
            .FirstOrDefaultAsync(ct);

    public async Task<Result<long>> CreateAccountAsync(CreateAccountDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Accounting.CreateAccount", async () =>
        {
            if (await _db.Set<Account>().AnyAsync(a => a.Code == dto.Code && !a.IsDeleted, ct))
                return Result<long>.Conflict(Errors.Accounting.AccountCodeExists);

            var entity = new Account
            {
                Name = dto.Name,
                Code = dto.Code,
                AccountGroupId = dto.AccountGroupId,
                OpeningBalance = dto.OpeningBalance,
                OpeningBalanceType = dto.OpeningBalanceType,
                GstNumber = dto.GstNumber,
                LinkedCustomerId = dto.LinkedCustomerId,
                LinkedSupplierId = dto.LinkedSupplierId,
                Description = dto.Description,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<Account>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> UpdateAccountAsync(long id, UpdateAccountDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Accounting.UpdateAccount", async () =>
        {
            var account = await _db.Set<Account>().FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct);
            if (account is null) return Result<bool>.NotFound(Errors.Accounting.AccountNotFound);
            if (account.IsSystem) return Result<bool>.Conflict(Errors.Accounting.SystemAccountReadOnly);

            account.Name = dto.Name;
            account.Description = dto.Description;
            account.IsActive = dto.IsActive;
            account.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    // ── Vouchers ──────────────────────────────────────────────────────────────

    public async Task<PagedResult<VoucherListDto>> ListVouchersAsync(
        int page, int pageSize, VoucherType? type, CancellationToken ct = default)
    {
        var query = _db.Set<Voucher>()
            .Where(v => !v.IsDeleted && (type == null || v.VoucherType == type));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(v => v.VoucherDate).ThenByDescending(v => v.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(v => new VoucherListDto(
                v.Id, v.VoucherNumber, v.VoucherDate, v.VoucherType, v.Status, v.TotalDebit, v.Narration))
            .ToListAsync(ct);

        return new PagedResult<VoucherListDto>(items, total, page, pageSize);
    }

    public async Task<VoucherDetailDto?> GetVoucherAsync(long id, CancellationToken ct = default)
        => await _db.Set<Voucher>()
            .Include(v => v.Entries).ThenInclude(e => e.Account)
            .Where(v => v.Id == id && !v.IsDeleted)
            .Select(v => (VoucherDetailDto?)new VoucherDetailDto(
                v.Id, v.VoucherNumber, v.VoucherDate, v.VoucherType, v.Status,
                v.TotalDebit, v.TotalCredit, v.Narration,
                v.SourceDocumentType, v.SourceDocumentId,
                v.Entries.Select(e => new VoucherEntryLineDto(
                    e.Id, e.AccountId, e.Account.Name, e.Type, e.Amount, e.Narration)).ToList()))
            .FirstOrDefaultAsync(ct);

    public async Task<Result<long>> CreateVoucherAsync(CreateVoucherDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Accounting.CreateVoucher", async () =>
        {
            var totalDebit  = dto.Entries.Where(e => e.Type == DebitCredit.Debit).Sum(e => e.Amount);
            var totalCredit = dto.Entries.Where(e => e.Type == DebitCredit.Credit).Sum(e => e.Amount);

            if (totalDebit != totalCredit)
                return Result<long>.Conflict(Errors.Accounting.VoucherImbalanced);

            var voucherNumber = await sequence.NextAsync(
                SequenceCodeFor(dto.VoucherType), tenant.ShopId, ct);

            var voucher = new Voucher
            {
                VoucherNumber = voucherNumber,
                VoucherDate = dto.VoucherDate,
                VoucherType = dto.VoucherType,
                Status = VoucherStatus.Draft,
                Narration = dto.Narration,
                TotalDebit = totalDebit,
                TotalCredit = totalCredit,
                SourceDocumentType = dto.SourceDocumentType,
                SourceDocumentId = dto.SourceDocumentId,
                IsPosted = false,
                CreatedAtUtc = DateTime.UtcNow,
            };

            foreach (var entry in dto.Entries)
            {
                voucher.Entries.Add(new VoucherEntry
                {
                    AccountId = entry.AccountId,
                    Type = entry.Type,
                    Amount = entry.Amount,
                    Narration = entry.Narration,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }

            _db.Set<Voucher>().Add(voucher);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(voucher.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> PostVoucherAsync(long id, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Accounting.PostVoucher", async () =>
        {
            var voucher = await _db.Set<Voucher>()
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, ct);
            if (voucher is null) return Result<bool>.NotFound(Errors.Accounting.VoucherNotFound);
            if (voucher.Status != VoucherStatus.Draft)
                return Result<bool>.Conflict(Errors.Accounting.VoucherAlreadyPosted);

            voucher.Status = VoucherStatus.Posted;
            voucher.IsPosted = true;
            voucher.PostedAtUtc = DateTime.UtcNow;
            voucher.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<long>> ReverseVoucherAsync(long id, string narration, CancellationToken ct = default)
        => await ExecuteAsync<long>("Accounting.ReverseVoucher", async () =>
        {
            var original = await _db.Set<Voucher>()
                .Include(v => v.Entries)
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, ct);
            if (original is null) return Result<long>.NotFound(Errors.Accounting.VoucherNotFound);
            if (original.Status != VoucherStatus.Posted)
                return Result<long>.Conflict(Errors.Accounting.VoucherNotPosted);

            var reversalNumber = await sequence.NextAsync(
                SequenceCodeFor(original.VoucherType), tenant.ShopId, ct);

            var reversal = new Voucher
            {
                VoucherNumber = reversalNumber,
                VoucherDate = DateTime.UtcNow.Date,
                VoucherType = original.VoucherType,
                Status = VoucherStatus.Posted,
                Narration = narration,
                TotalDebit = original.TotalCredit,
                TotalCredit = original.TotalDebit,
                SourceDocumentType = "Reversal",
                SourceDocumentId = original.Id,
                IsPosted = true,
                PostedAtUtc = DateTime.UtcNow,
                ReversedByVoucherId = original.Id,
                CreatedAtUtc = DateTime.UtcNow,
            };

            foreach (var entry in original.Entries)
            {
                reversal.Entries.Add(new VoucherEntry
                {
                    AccountId = entry.AccountId,
                    Type = entry.Type == DebitCredit.Debit ? DebitCredit.Credit : DebitCredit.Debit,
                    Amount = entry.Amount,
                    Narration = $"Reversal: {entry.Narration}",
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }

            original.Status = VoucherStatus.Reversed;
            _db.Set<Voucher>().Add(reversal);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(reversal.Id);
        }, ct, useTransaction: true);

    // ── Expenses ──────────────────────────────────────────────────────────────

    public async Task<PagedResult<ExpenseListDto>> ListExpensesAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Set<Expense>().Where(e => !e.IsDeleted);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.ExpenseDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new ExpenseListDto(e.Id, e.ExpenseDate, e.Description, e.Amount, e.PaymentModeCode, e.VoucherId))
            .ToListAsync(ct);
        return new PagedResult<ExpenseListDto>(items, total, page, pageSize);
    }

    public async Task<Result<long>> CreateExpenseAsync(CreateExpenseDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Accounting.CreateExpense", async () =>
        {
            var expense = new Expense
            {
                ExpenseDate = dto.ExpenseDate,
                AccountId = dto.AccountId,
                Description = dto.Description,
                Amount = dto.Amount,
                PaymentModeCode = dto.PaymentModeCode,
                PaidFromAccountId = dto.PaidFromAccountId,
                IsRecurring = dto.IsRecurring,
                RecurrenceInterval = dto.RecurrenceInterval,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<Expense>().Add(expense);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(expense.Id);
        }, ct, useTransaction: true);

    // ── Bank Accounts ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<BankAccountDto>> ListBankAccountsAsync(CancellationToken ct = default)
        => await _db.Set<BankAccount>()
            .Where(b => !b.IsDeleted && b.IsActive)
            .Select(b => new BankAccountDto(b.Id, b.AccountId, b.BankName, b.AccountNumber,
                b.IfscCode, b.AccountHolderName, b.IsActive))
            .ToListAsync(ct);

    public async Task<Result<long>> CreateBankAccountAsync(CreateBankAccountDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Accounting.CreateBankAccount", async () =>
        {
            if (await _db.Set<BankAccount>().AnyAsync(b => b.AccountNumber == dto.AccountNumber && !b.IsDeleted, ct))
                return Result<long>.Conflict(Errors.Accounting.BankAccountExists);

            var entity = new BankAccount
            {
                AccountId = dto.AccountId,
                BankName = dto.BankName,
                AccountNumber = dto.AccountNumber,
                IfscCode = dto.IfscCode,
                BranchName = dto.BranchName,
                AccountHolderName = dto.AccountHolderName,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<BankAccount>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);

    // ── Financial Year ────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<FinancialYearDto>> ListFinancialYearsAsync(CancellationToken ct = default)
        => await _db.Set<FinancialYear>()
            .Where(f => !f.IsDeleted)
            .OrderByDescending(f => f.StartYear)
            .Select(f => new FinancialYearDto(f.Id, f.StartYear, f.StartDate, f.EndDate, f.IsClosed, f.ClosedAtUtc))
            .ToListAsync(ct);

    public async Task<Result<long>> CreateFinancialYearAsync(CreateFinancialYearDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Accounting.CreateFinancialYear", async () =>
        {
            if (await _db.Set<FinancialYear>().AnyAsync(f => f.StartYear == dto.StartYear && !f.IsDeleted, ct))
                return Result<long>.Conflict(Errors.Accounting.FinancialYearExists);

            var startDate = new DateTime(dto.StartYear, 4, 1);
            var entity = new FinancialYear
            {
                StartYear = dto.StartYear,
                StartDate = startDate,
                EndDate = startDate.AddYears(1).AddDays(-1),
                IsClosed = false,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<FinancialYear>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> CloseFinancialYearAsync(long id, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Accounting.CloseFinancialYear", async () =>
        {
            var fy = await _db.Set<FinancialYear>().FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, ct);
            if (fy is null) return Result<bool>.NotFound(Errors.Accounting.FinancialYearNotFound);
            if (fy.IsClosed) return Result<bool>.Conflict(Errors.Accounting.FinancialYearAlreadyClosed);

            var hasOpenVouchers = await _db.Set<Voucher>()
                .AnyAsync(v => v.FinancialYearId == id && v.Status == VoucherStatus.Draft && !v.IsDeleted, ct);
            if (hasOpenVouchers)
                return Result<bool>.Conflict(Errors.Accounting.FinancialYearHasOpenVouchers);

            fy.IsClosed = true;
            fy.ClosedAtUtc = DateTime.UtcNow;
            fy.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    // ── IAutoVoucherService ───────────────────────────────────────────────────

    public async Task<Result<long>> PostSaleVoucherAsync(
        long shopId, long invoiceId, string invoiceNumber, decimal saleAmount,
        decimal taxAmount, CancellationToken ct = default)
    {
        var cashId  = await GetSystemAccountIdAsync("1010", ct);
        var salesId = await GetSystemAccountIdAsync("4000", ct);
        var total   = saleAmount + taxAmount;

        var entries = new List<VoucherEntryDto>
        {
            new(cashId,  DebitCredit.Debit,  total,      invoiceNumber),
            new(salesId, DebitCredit.Credit, saleAmount, invoiceNumber),
        };

        if (taxAmount > 0)
        {
            var cgstId = await GetSystemAccountIdAsync("2200", ct);
            var sgstId = await GetSystemAccountIdAsync("2201", ct);
            var half   = Math.Round(taxAmount / 2, 2);
            entries.Add(new(cgstId, DebitCredit.Credit, half,            invoiceNumber));
            entries.Add(new(sgstId, DebitCredit.Credit, taxAmount - half, invoiceNumber));
        }

        return await CreateVoucherAsync(new CreateVoucherDto(
            DateTime.UtcNow.Date, VoucherType.Receipt,
            $"Sale — {invoiceNumber}", "Invoice", invoiceId, entries), ct);
    }

    public async Task<Result<long>> PostPaymentReceivedVoucherAsync(
        long shopId, long invoiceId, string invoiceNumber, decimal amount,
        string paymentMode, CancellationToken ct = default)
    {
        var isBank  = paymentMode.Equals("Card",  StringComparison.OrdinalIgnoreCase)
                   || paymentMode.Equals("Bank",  StringComparison.OrdinalIgnoreCase)
                   || paymentMode.Equals("Cheque", StringComparison.OrdinalIgnoreCase);
        var debitId = await GetSystemAccountIdAsync(isBank ? "1020" : "1010", ct);
        var arId    = await GetSystemAccountIdAsync("1100", ct);

        return await CreateVoucherAsync(new CreateVoucherDto(
            DateTime.UtcNow.Date, VoucherType.Receipt,
            $"Payment received — {invoiceNumber} ({paymentMode})", "Invoice", invoiceId,
            [
                new(debitId, DebitCredit.Debit,  amount, invoiceNumber),
                new(arId,    DebitCredit.Credit, amount, invoiceNumber),
            ]), ct);
    }

    public async Task<Result<long>> PostExpenseVoucherAsync(
        long shopId, long expenseId, long expenseAccountId, long cashAccountId,
        decimal amount, string narration, CancellationToken ct = default)
    {
        return await CreateVoucherAsync(new CreateVoucherDto(
            DateTime.UtcNow.Date, VoucherType.Journal, narration,
            "Expense", expenseId,
            [
                new(expenseAccountId, DebitCredit.Debit,  amount, narration),
                new(cashAccountId,    DebitCredit.Credit, amount, narration),
            ]), ct);
    }

    public async Task<Result<long>> PostShiftVarianceVoucherAsync(
        long shopId, long shiftId, decimal variance, CancellationToken ct = default)
    {
        if (variance == 0) return Result<long>.Success(0L);

        var cashId  = await GetSystemAccountIdAsync("1010", ct);
        var narration = $"Shift #{shiftId} variance";

        List<VoucherEntryDto> entries;
        if (variance > 0)
        {
            var overId = await GetSystemAccountIdAsync("4810", ct);
            entries =
            [
                new(cashId,  DebitCredit.Debit,  variance, narration),
                new(overId,  DebitCredit.Credit, variance, narration),
            ];
        }
        else
        {
            var shortId = await GetSystemAccountIdAsync("5810", ct);
            var abs = Math.Abs(variance);
            entries =
            [
                new(shortId, DebitCredit.Debit,  abs, narration),
                new(cashId,  DebitCredit.Credit, abs, narration),
            ];
        }

        return await CreateVoucherAsync(new CreateVoucherDto(
            DateTime.UtcNow.Date, VoucherType.Journal,
            narration, "Shift", shiftId, entries), ct);
    }

    public async Task<Result<long>> PostPurchaseBillVoucherAsync(
        long shopId, long billId, string billNumber, decimal totalAmount, CancellationToken ct = default)
    {
        var inventoryId = await GetSystemAccountIdAsync("1200", ct);
        var apId        = await GetSystemAccountIdAsync("2100", ct);

        return await CreateVoucherAsync(new CreateVoucherDto(
            DateTime.UtcNow.Date, VoucherType.Journal,
            $"Purchase bill — {billNumber}", "Bill", billId,
            [
                new(inventoryId, DebitCredit.Debit,  totalAmount, billNumber),
                new(apId,        DebitCredit.Credit, totalAmount, billNumber),
            ]), ct);
    }

    public async Task<Result<long>> PostSalesReturnVoucherAsync(
        long shopId, long salesReturnId, string returnNumber, decimal totalRefundAmount, CancellationToken ct = default)
    {
        var returnsId = await GetSystemAccountIdAsync("4010", ct);
        var cashId    = await GetSystemAccountIdAsync("1010", ct);

        return await CreateVoucherAsync(new CreateVoucherDto(
            DateTime.UtcNow.Date, VoucherType.Journal,
            $"Sales return — {returnNumber}", "SalesReturn", salesReturnId,
            [
                new(returnsId, DebitCredit.Debit,  totalRefundAmount, returnNumber),
                new(cashId,    DebitCredit.Credit, totalRefundAmount, returnNumber),
            ]), ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<long> GetSystemAccountIdAsync(string code, CancellationToken ct)
    {
        var id = await _db.Set<Account>()
            .Where(a => a.Code == code && !a.IsDeleted && a.IsActive)
            .Select(a => (long?)a.Id)
            .FirstOrDefaultAsync(ct);
        if (id is null or 0)
            throw new InvalidOperationException(
                $"System COA account '{code}' not found — ensure AccountingTenantSeeder has run for this shop.");
        return id.Value;
    }

    private static string SequenceCodeFor(VoucherType type) => type switch
    {
        VoucherType.Payment  => Constants.SequenceCodes.VoucherPayment,
        VoucherType.Receipt  => Constants.SequenceCodes.VoucherReceipt,
        VoucherType.Contra   => Constants.SequenceCodes.VoucherContra,
        _                    => Constants.SequenceCodes.VoucherJournal,
    };
}
