using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Accounting.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record AccountGroupDto(
    long Id,
    string Name,
    string Code,
    long? ParentId,
    AccountNature Nature,
    bool IsSystem,
    int SortOrder);

public record AccountListDto(
    long Id,
    string Name,
    string Code,
    long AccountGroupId,
    string AccountGroupName,
    AccountNature Nature,
    bool IsSystem,
    bool IsActive,
    decimal OpeningBalance,
    DebitCredit OpeningBalanceType);

public record CreateAccountDto(
    string Name,
    string Code,
    long AccountGroupId,
    decimal OpeningBalance,
    DebitCredit OpeningBalanceType,
    string? GstNumber = null,
    long? LinkedCustomerId = null,
    long? LinkedSupplierId = null,
    string? Description = null);

public record UpdateAccountDto(
    string Name,
    string? Description,
    bool IsActive);

public record VoucherEntryDto(
    long AccountId,
    DebitCredit Type,
    decimal Amount,
    string? Narration);

public record CreateVoucherDto(
    DateTime VoucherDate,
    VoucherType VoucherType,
    string? Narration,
    string? SourceDocumentType,
    long? SourceDocumentId,
    IReadOnlyList<VoucherEntryDto> Entries);

public record VoucherListDto(
    long Id,
    string VoucherNumber,
    DateTime VoucherDate,
    VoucherType VoucherType,
    VoucherStatus Status,
    decimal TotalDebit,
    string? Narration);

public record VoucherDetailDto(
    long Id,
    string VoucherNumber,
    DateTime VoucherDate,
    VoucherType VoucherType,
    VoucherStatus Status,
    decimal TotalDebit,
    decimal TotalCredit,
    string? Narration,
    string? SourceDocumentType,
    long? SourceDocumentId,
    IReadOnlyList<VoucherEntryLineDto> Entries);

public record VoucherEntryLineDto(
    long Id,
    long AccountId,
    string AccountName,
    DebitCredit Type,
    decimal Amount,
    string? Narration);

public record CreateExpenseDto(
    DateTime ExpenseDate,
    long AccountId,
    string Description,
    decimal Amount,
    string PaymentModeCode,
    long? PaidFromAccountId,
    bool IsRecurring = false,
    ExpenseRecurrenceInterval? RecurrenceInterval = null);

public record ExpenseListDto(
    long Id,
    DateTime ExpenseDate,
    string Description,
    decimal Amount,
    string PaymentModeCode,
    long? VoucherId);

public record CreateBankAccountDto(
    long AccountId,
    string BankName,
    string AccountNumber,
    string IfscCode,
    string BranchName,
    string AccountHolderName);

public record BankAccountDto(
    long Id,
    long AccountId,
    string BankName,
    string AccountNumber,
    string IfscCode,
    string AccountHolderName,
    bool IsActive);

public record FinancialYearDto(
    long Id,
    int StartYear,
    DateTime StartDate,
    DateTime EndDate,
    bool IsClosed,
    DateTime? ClosedAtUtc);

public record CreateFinancialYearDto(int StartYear);

// ── Interfaces ─────────────────────────────────────────────────────────────────

public interface IAccountingService
{
    // Account Groups
    Task<IReadOnlyList<AccountGroupDto>> ListAccountGroupsAsync(CancellationToken ct = default);

    // Accounts
    Task<PagedResult<AccountListDto>> ListAccountsAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<AccountListDto?> GetAccountAsync(long id, CancellationToken ct = default);
    Task<Result<long>> CreateAccountAsync(CreateAccountDto dto, CancellationToken ct = default);
    Task<Result<bool>> UpdateAccountAsync(long id, UpdateAccountDto dto, CancellationToken ct = default);

    // Vouchers
    Task<PagedResult<VoucherListDto>> ListVouchersAsync(int page, int pageSize, VoucherType? type, CancellationToken ct = default);
    Task<VoucherDetailDto?> GetVoucherAsync(long id, CancellationToken ct = default);
    Task<Result<long>> CreateVoucherAsync(CreateVoucherDto dto, CancellationToken ct = default);
    Task<Result<bool>> PostVoucherAsync(long id, CancellationToken ct = default);
    Task<Result<long>> ReverseVoucherAsync(long id, string narration, CancellationToken ct = default);

    // Expenses
    Task<PagedResult<ExpenseListDto>> ListExpensesAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<long>> CreateExpenseAsync(CreateExpenseDto dto, CancellationToken ct = default);

    // Bank Accounts
    Task<IReadOnlyList<BankAccountDto>> ListBankAccountsAsync(CancellationToken ct = default);
    Task<Result<long>> CreateBankAccountAsync(CreateBankAccountDto dto, CancellationToken ct = default);

    // Financial Year
    Task<IReadOnlyList<FinancialYearDto>> ListFinancialYearsAsync(CancellationToken ct = default);
    Task<Result<long>> CreateFinancialYearAsync(CreateFinancialYearDto dto, CancellationToken ct = default);
    Task<Result<bool>> CloseFinancialYearAsync(long id, CancellationToken ct = default);
}
