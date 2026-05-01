namespace ErpSaas.Modules.Reports.Services;

public interface IReportQueryRepository
{
    Task<IReadOnlyList<TrialBalanceRow>> QueryTrialBalanceAsync(long shopId, DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<ProfitLossRow>> QueryProfitLossAsync(long shopId, DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<BalanceSheetRow>> QueryBalanceSheetAsync(long shopId, DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<DayBookEntry>> QueryDayBookAsync(long shopId, DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<LedgerEntry>> QueryLedgerAsync(long shopId, long accountId, DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<GstR1B2bRow>> QueryGstr1B2bAsync(long shopId, DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<HsnSummaryRow>> QueryHsnSummaryAsync(long shopId, DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<Gstr3bRow>> QueryGstr3bAsync(long shopId, DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<CashBookEntry>> QueryCashBookAsync(long shopId, DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<BankBookEntry>> QueryBankBookAsync(long shopId, long bankAccountId, DateRangeParams p, CancellationToken ct = default);
    Task<IReadOnlyList<WalletStatementEntry>> QueryWalletStatementAsync(long shopId, long customerId, DateRangeParams p, CancellationToken ct = default);
}
