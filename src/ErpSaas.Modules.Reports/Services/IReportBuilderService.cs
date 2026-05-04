using ErpSaas.Modules.Reports.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Reports.Services;

// ── Report parameter records ───────────────────────────────────────────────────

public record DateRangeParams(DateTime From, DateTime To, long? BranchId = null);

// ── Result models ─────────────────────────────────────────────────────────────

public record TrialBalanceRow(
    long AccountId, string AccountCode, string AccountName, string GroupName,
    decimal Debit, decimal Credit, decimal ClosingBalance);

public record ProfitLossRow(
    string Category, string AccountName, decimal Amount, bool IsExpense);

public record BalanceSheetRow(
    string Side, string GroupName, string AccountName, decimal Amount);

public record DayBookEntry(
    DateTime Date, string VoucherNumber, string VoucherType,
    string Particulars, decimal Debit, decimal Credit);

public record LedgerEntry(
    DateTime Date, string VoucherNumber, string Particulars,
    decimal Debit, decimal Credit, decimal RunningBalance);

public record GstR1B2bRow(
    string CustomerGstin, string CustomerName, string InvoiceNumber,
    DateTime InvoiceDate, decimal TaxableValue, decimal CgstRate, decimal CgstAmount,
    decimal SgstRate, decimal SgstAmount, decimal IgstRate, decimal IgstAmount,
    decimal InvoiceValue);

public record HsnSummaryRow(
    string HsnCode, string Description, string UomCode,
    decimal TotalQuantity, decimal TaxableValue, decimal CgstAmount,
    decimal SgstAmount, decimal IgstAmount, decimal TotalTax);

public record Gstr3bRow(
    string Section, string Description, decimal TaxableValue,
    decimal CgstAmount, decimal SgstAmount, decimal IgstAmount, decimal TotalTax);

public record CashBookEntry(
    DateTime Date, string VoucherNumber, string Particulars,
    decimal Receipts, decimal Payments, decimal RunningBalance);

public record BankBookEntry(
    DateTime Date, string VoucherNumber, string Particulars, string BankName,
    decimal Deposits, decimal Withdrawals, decimal RunningBalance);

public record WalletStatementEntry(
    DateTime Date, string TransactionType, string Particulars,
    decimal Credit, decimal Debit, decimal RunningBalance);

public record PaymentSummaryRow(
    DateTime Date, string GatewayCode, string? Method,
    int TotalCount, int SuccessCount, int FailedCount,
    decimal TotalAmount, decimal SuccessAmount,
    decimal SuccessRatePct);

public record FailedPaymentRow(
    DateTime InitiatedAtUtc, string GatewayCode, string OurReferenceNumber,
    decimal Amount, string? FailureCode, string? FailureMessage,
    string Purpose);

public record SettlementGapRow(
    string GatewayCode, string GatewayTxnId, string OurReferenceNumber,
    decimal Amount, decimal GatewayFee, decimal NetSettled,
    DateTime CompletedAtUtc, DateTime? SettledAtUtc,
    double? GapDays);

public record ReconciliationExceptionRow(
    DateTime DetectedAtUtc, string GatewayCode, string? GatewayTxnId,
    string ExceptionType, string Status,
    decimal? OurAmount, decimal? GatewayAmount,
    double AgeDays, string? ResolutionNotes);

public record ReportFile(string FileName, string ContentType, byte[] Data);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IReportBuilderService
{
    // Accounting reports
    Task<IReadOnlyList<TrialBalanceRow>> GetTrialBalanceAsync(DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<ProfitLossRow>> GetProfitLossAsync(DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<BalanceSheetRow>> GetBalanceSheetAsync(DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<DayBookEntry>> GetDayBookAsync(DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<LedgerEntry>> GetLedgerAsync(long accountId, DateRangeParams p, CancellationToken ct = default);

    // GST reports
    Task<IReadOnlyList<GstR1B2bRow>> GetGstr1B2bAsync(DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<HsnSummaryRow>> GetHsnSummaryAsync(DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<Gstr3bRow>> GetGstr3bAsync(DateRangeParams p, CancellationToken ct = default);

    // Cash / bank / wallet
    Task<IReadOnlyList<CashBookEntry>> GetCashBookAsync(DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<BankBookEntry>> GetBankBookAsync(long bankAccountId, DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<WalletStatementEntry>> GetWalletStatementAsync(long customerId, DateRangeParams p, CancellationToken ct = default);

    // Payment reports
    Task<IReadOnlyList<PaymentSummaryRow>> GetPaymentSummaryAsync(DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<FailedPaymentRow>> GetFailedPaymentsAsync(DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<SettlementGapRow>> GetSettlementGapAsync(DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<ReconciliationExceptionRow>> GetReconciliationExceptionsAsync(DateRangeParams p, CancellationToken ct = default);

    // Export
    Task<Result<ReportFile>> ExportAsync(string reportCode, DateRangeParams p, ReportFormat format, long? accountId = null, CancellationToken ct = default);
}
