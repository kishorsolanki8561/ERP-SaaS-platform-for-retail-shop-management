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

public sealed class AccountingService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence,
    ITenantContext tenant,
    ILogger<AccountingService> logger)
    : BaseService<TenantDbContext>(db, errorLogger), IAccountingService, IAutoVoucherService
{
    // ── Account Groups ────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<AccountGroupDto>> ListAccountGroupsAsync(CancellationToken ct = default)
        => await db.Set<AccountGroup>()
            .Where(g => !g.IsDeleted)
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Name)
            .Select(g => new AccountGroupDto(g.Id, g.Name, g.Code, g.ParentId, g.Nature, g.IsSystem, g.SortOrder))
            .ToListAsync(ct);

    // ── Accounts ──────────────────────────────────────────────────────────────

    public async Task<PagedResult<AccountListDto>> ListAccountsAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = db.Set<Account>()
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
        => await db.Set<Account>()
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
            if (await db.Set<Account>().AnyAsync(a => a.Code == dto.Code && !a.IsDeleted, ct))
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
            db.Set<Account>().Add(entity);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> UpdateAccountAsync(long id, UpdateAccountDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Accounting.UpdateAccount", async () =>
        {
            var account = await db.Set<Account>().FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct);
            if (account is null) return Result<bool>.NotFound(Errors.Accounting.AccountNotFound);
            if (account.IsSystem) return Result<bool>.Conflict(Errors.Accounting.SystemAccountReadOnly);

            account.Name = dto.Name;
            account.Description = dto.Description;
            account.IsActive = dto.IsActive;
            account.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    // ── Vouchers ──────────────────────────────────────────────────────────────

    public async Task<PagedResult<VoucherListDto>> ListVouchersAsync(
        int page, int pageSize, VoucherType? type, CancellationToken ct = default)
    {
        var query = db.Set<Voucher>()
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
        => await db.Set<Voucher>()
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

            db.Set<Voucher>().Add(voucher);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(voucher.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> PostVoucherAsync(long id, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Accounting.PostVoucher", async () =>
        {
            var voucher = await db.Set<Voucher>()
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, ct);
            if (voucher is null) return Result<bool>.NotFound(Errors.Accounting.VoucherNotFound);
            if (voucher.Status != VoucherStatus.Draft)
                return Result<bool>.Conflict(Errors.Accounting.VoucherAlreadyPosted);

            voucher.Status = VoucherStatus.Posted;
            voucher.IsPosted = true;
            voucher.PostedAtUtc = DateTime.UtcNow;
            voucher.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<long>> ReverseVoucherAsync(long id, string narration, CancellationToken ct = default)
        => await ExecuteAsync<long>("Accounting.ReverseVoucher", async () =>
        {
            var original = await db.Set<Voucher>()
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
            db.Set<Voucher>().Add(reversal);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(reversal.Id);
        }, ct, useTransaction: true);

    // ── Expenses ──────────────────────────────────────────────────────────────

    public async Task<PagedResult<ExpenseListDto>> ListExpensesAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Set<Expense>().Where(e => !e.IsDeleted);
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
            db.Set<Expense>().Add(expense);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(expense.Id);
        }, ct, useTransaction: true);

    // ── Bank Accounts ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<BankAccountDto>> ListBankAccountsAsync(CancellationToken ct = default)
        => await db.Set<BankAccount>()
            .Where(b => !b.IsDeleted && b.IsActive)
            .Select(b => new BankAccountDto(b.Id, b.AccountId, b.BankName, b.AccountNumber,
                b.IfscCode, b.AccountHolderName, b.IsActive))
            .ToListAsync(ct);

    public async Task<Result<long>> CreateBankAccountAsync(CreateBankAccountDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Accounting.CreateBankAccount", async () =>
        {
            if (await db.Set<BankAccount>().AnyAsync(b => b.AccountNumber == dto.AccountNumber && !b.IsDeleted, ct))
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
            db.Set<BankAccount>().Add(entity);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);

    // ── Financial Year ────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<FinancialYearDto>> ListFinancialYearsAsync(CancellationToken ct = default)
        => await db.Set<FinancialYear>()
            .Where(f => !f.IsDeleted)
            .OrderByDescending(f => f.StartYear)
            .Select(f => new FinancialYearDto(f.Id, f.StartYear, f.StartDate, f.EndDate, f.IsClosed, f.ClosedAtUtc))
            .ToListAsync(ct);

    public async Task<Result<long>> CreateFinancialYearAsync(CreateFinancialYearDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Accounting.CreateFinancialYear", async () =>
        {
            if (await db.Set<FinancialYear>().AnyAsync(f => f.StartYear == dto.StartYear && !f.IsDeleted, ct))
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
            db.Set<FinancialYear>().Add(entity);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> CloseFinancialYearAsync(long id, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Accounting.CloseFinancialYear", async () =>
        {
            var fy = await db.Set<FinancialYear>().FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, ct);
            if (fy is null) return Result<bool>.NotFound(Errors.Accounting.FinancialYearNotFound);
            if (fy.IsClosed) return Result<bool>.Conflict(Errors.Accounting.FinancialYearAlreadyClosed);

            var hasOpenVouchers = await db.Set<Voucher>()
                .AnyAsync(v => v.FinancialYearId == id && v.Status == VoucherStatus.Draft && !v.IsDeleted, ct);
            if (hasOpenVouchers)
                return Result<bool>.Conflict(Errors.Accounting.FinancialYearHasOpenVouchers);

            fy.IsClosed = true;
            fy.ClosedAtUtc = DateTime.UtcNow;
            fy.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    // ── IAutoVoucherService ───────────────────────────────────────────────────

    public async Task<Result<long>> PostSaleVoucherAsync(
        long shopId, long invoiceId, string invoiceNumber, decimal saleAmount,
        decimal taxAmount, CancellationToken ct = default)
    {
        // TODO: resolve actual account IDs from Chart of Accounts (Cash/Sales/GST Payable).
        // Placeholder implementation until COA seeder wires up system account IDs.
        logger.LogInformation(
            "Auto-voucher: Sale {InvoiceNumber} amount {Amount} tax {Tax} — COA accounts pending",
            invoiceNumber, saleAmount, taxAmount);
        return await Task.FromResult(Result<long>.Success(0L));
    }

    public async Task<Result<long>> PostPaymentReceivedVoucherAsync(
        long shopId, long invoiceId, string invoiceNumber, decimal amount,
        string paymentMode, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Auto-voucher: Payment received for {InvoiceNumber} amount {Amount} mode {Mode}",
            invoiceNumber, amount, paymentMode);
        return await Task.FromResult(Result<long>.Success(0L));
    }

    public async Task<Result<long>> PostExpenseVoucherAsync(
        long shopId, long expenseId, long expenseAccountId, long cashAccountId,
        decimal amount, string narration, CancellationToken ct = default)
    {
        var dto = new CreateVoucherDto(
            VoucherDate: DateTime.UtcNow.Date,
            VoucherType: VoucherType.Journal,
            Narration: narration,
            SourceDocumentType: "Expense",
            SourceDocumentId: expenseId,
            Entries:
            [
                new(expenseAccountId, DebitCredit.Debit, amount, narration),
                new(cashAccountId, DebitCredit.Credit, amount, narration),
            ]);
        return await CreateVoucherAsync(dto, ct);
    }

    public async Task<Result<long>> PostShiftVarianceVoucherAsync(
        long shopId, long shiftId, decimal variance, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Auto-voucher: Shift {ShiftId} variance {Variance} — COA accounts pending",
            shiftId, variance);
        return await Task.FromResult(Result<long>.Success(0L));
    }

    public async Task<Result<long>> PostPurchaseBillVoucherAsync(
        long shopId, long billId, string billNumber, decimal totalAmount, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Auto-voucher: Purchase bill {BillNumber} amount {Amount} — AP entry pending COA",
            billNumber, totalAmount);
        return await Task.FromResult(Result<long>.Success(0L));
    }

    public async Task<Result<long>> PostSalesReturnVoucherAsync(
        long shopId, long salesReturnId, string returnNumber, decimal totalRefundAmount, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Auto-voucher: Sales return {ReturnNumber} refund {Amount} — reversal entry pending COA",
            returnNumber, totalRefundAmount);
        return await Task.FromResult(Result<long>.Success(0L));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string SequenceCodeFor(VoucherType type) => type switch
    {
        VoucherType.Payment  => Constants.SequenceCodes.VoucherPayment,
        VoucherType.Receipt  => Constants.SequenceCodes.VoucherReceipt,
        VoucherType.Contra   => Constants.SequenceCodes.VoucherContra,
        _                    => Constants.SequenceCodes.VoucherJournal,
    };
}
