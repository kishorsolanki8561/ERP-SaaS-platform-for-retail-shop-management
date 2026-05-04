using Dapper;
using ErpSaas.Infrastructure.Dapper;

namespace ErpSaas.Modules.Reports.Services;

public sealed class ReportQueryRepository(IDapperContext dapper) : IReportQueryRepository
{
    public async Task<IReadOnlyList<TrialBalanceRow>> QueryTrialBalanceAsync(long shopId, DateRangeParams p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                a.Id            AS AccountId,
                a.Code          AS AccountCode,
                a.Name          AS AccountName,
                g.Name          AS GroupName,
                SUM(CASE WHEN ve.[Type] = 'Debit'  THEN ve.Amount ELSE 0 END) AS Debit,
                SUM(CASE WHEN ve.[Type] = 'Credit' THEN ve.Amount ELSE 0 END) AS Credit,
                SUM(CASE WHEN ve.[Type] = 'Debit'  THEN ve.Amount ELSE -ve.Amount END) AS ClosingBalance
            FROM accounting.Account a
            INNER JOIN accounting.AccountGroup g ON g.Id = a.AccountGroupId
            INNER JOIN accounting.VoucherEntry ve ON ve.AccountId = a.Id
            INNER JOIN accounting.Voucher v ON v.Id = ve.VoucherId
            WHERE a.ShopId = @ShopId
              AND v.ShopId = @ShopId
              AND v.VoucherDate >= @From
              AND v.VoucherDate <= @To
              AND v.Status = 'Posted'
            GROUP BY a.Id, a.Code, a.Name, g.Name
            ORDER BY g.Name, a.Code
            """;
        var rows = await dapper.Connection.QueryAsync<TrialBalanceRow>(sql, new { ShopId = shopId, p.From, p.To });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ProfitLossRow>> QueryProfitLossAsync(long shopId, DateRangeParams p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                g.Name          AS Category,
                a.Name          AS AccountName,
                SUM(CASE WHEN ve.[Type] = 'Debit'  THEN ve.Amount ELSE -ve.Amount END) AS Amount,
                CAST(CASE WHEN g.Nature IN ('Expense') THEN 1 ELSE 0 END AS BIT) AS IsExpense
            FROM accounting.Account a
            INNER JOIN accounting.AccountGroup g ON g.Id = a.AccountGroupId
            INNER JOIN accounting.VoucherEntry ve ON ve.AccountId = a.Id
            INNER JOIN accounting.Voucher v ON v.Id = ve.VoucherId
            WHERE a.ShopId = @ShopId
              AND v.ShopId = @ShopId
              AND v.VoucherDate >= @From
              AND v.VoucherDate <= @To
              AND v.Status = 'Posted'
              AND g.Nature IN ('Income','Expense')
            GROUP BY g.Name, a.Name, g.Nature
            ORDER BY IsExpense, g.Name, a.Name
            """;
        var rows = await dapper.Connection.QueryAsync<ProfitLossRow>(sql, new { ShopId = shopId, p.From, p.To });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<BalanceSheetRow>> QueryBalanceSheetAsync(long shopId, DateRangeParams p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                CASE WHEN g.Nature = 'Asset'     THEN 'Assets'
                     WHEN g.Nature IN ('Liability','Equity') THEN 'Liabilities & Equity'
                     ELSE g.Nature END AS Side,
                g.Name          AS GroupName,
                a.Name          AS AccountName,
                SUM(CASE WHEN ve.[Type] = 'Debit'  THEN ve.Amount ELSE -ve.Amount END) AS Amount
            FROM accounting.Account a
            INNER JOIN accounting.AccountGroup g ON g.Id = a.AccountGroupId
            INNER JOIN accounting.VoucherEntry ve ON ve.AccountId = a.Id
            INNER JOIN accounting.Voucher v ON v.Id = ve.VoucherId
            WHERE a.ShopId = @ShopId
              AND v.ShopId = @ShopId
              AND v.VoucherDate <= @To
              AND v.Status = 'Posted'
              AND g.Nature IN ('Asset','Liability','Equity')
            GROUP BY g.Nature, g.Name, a.Name
            ORDER BY g.Nature, g.Name, a.Name
            """;
        var rows = await dapper.Connection.QueryAsync<BalanceSheetRow>(sql, new { ShopId = shopId, p.To });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<DayBookEntry>> QueryDayBookAsync(long shopId, DateRangeParams p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                v.VoucherDate   AS Date,
                v.VoucherNumber AS VoucherNumber,
                v.VoucherType   AS VoucherType,
                v.Narration     AS Particulars,
                SUM(CASE WHEN ve.[Type] = 'Debit'  THEN ve.Amount ELSE 0 END) AS Debit,
                SUM(CASE WHEN ve.[Type] = 'Credit' THEN ve.Amount ELSE 0 END) AS Credit
            FROM accounting.Voucher v
            INNER JOIN accounting.VoucherEntry ve ON ve.VoucherId = v.Id
            WHERE v.ShopId = @ShopId
              AND v.VoucherDate >= @From
              AND v.VoucherDate <= @To
              AND v.Status = 'Posted'
            GROUP BY v.Id, v.VoucherDate, v.VoucherNumber, v.VoucherType, v.Narration
            ORDER BY v.VoucherDate, v.VoucherNumber
            """;
        var rows = await dapper.Connection.QueryAsync<DayBookEntry>(sql, new { ShopId = shopId, p.From, p.To });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<LedgerEntry>> QueryLedgerAsync(long shopId, long accountId, DateRangeParams p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                v.VoucherDate   AS Date,
                v.VoucherNumber AS VoucherNumber,
                v.Narration     AS Particulars,
                CASE WHEN ve.[Type] = 'Debit'  THEN ve.Amount ELSE 0 END AS Debit,
                CASE WHEN ve.[Type] = 'Credit' THEN ve.Amount ELSE 0 END AS Credit,
                SUM(CASE WHEN ve2.[Type] = 'Debit' THEN ve2.Amount ELSE -ve2.Amount END)
                    OVER (ORDER BY v.VoucherDate, v.Id ROWS UNBOUNDED PRECEDING) AS RunningBalance
            FROM accounting.VoucherEntry ve
            INNER JOIN accounting.Voucher v ON v.Id = ve.VoucherId
            INNER JOIN accounting.VoucherEntry ve2 ON ve2.VoucherId = ve.VoucherId AND ve2.AccountId = @AccountId
            WHERE ve.AccountId = @AccountId
              AND v.ShopId = @ShopId
              AND v.VoucherDate >= @From
              AND v.VoucherDate <= @To
              AND v.Status = 'Posted'
            ORDER BY v.VoucherDate, v.VoucherNumber
            """;
        var rows = await dapper.Connection.QueryAsync<LedgerEntry>(sql, new { AccountId = accountId, ShopId = shopId, p.From, p.To });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<GstR1B2bRow>> QueryGstr1B2bAsync(long shopId, DateRangeParams p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                c.GstNumber         AS CustomerGstin,
                c.DisplayName       AS CustomerName,
                i.InvoiceNumber     AS InvoiceNumber,
                i.InvoiceDate       AS InvoiceDate,
                SUM(il.TaxableAmount) AS TaxableValue,
                il.GstRate / 2      AS CgstRate,
                SUM(il.CgstAmount)  AS CgstAmount,
                il.GstRate / 2      AS SgstRate,
                SUM(il.SgstAmount)  AS SgstAmount,
                CAST(0 AS DECIMAL(5,2)) AS IgstRate,
                SUM(il.IgstAmount)  AS IgstAmount,
                i.GrandTotal        AS InvoiceValue
            FROM sales.Invoice i
            INNER JOIN crm.Customer c ON c.Id = i.CustomerId
            INNER JOIN sales.InvoiceLine il ON il.InvoiceId = i.Id
            WHERE i.ShopId = @ShopId
              AND i.InvoiceDate >= @From
              AND i.InvoiceDate <= @To
              AND i.Status IN ('Finalized','Paid')
              AND c.GstNumber IS NOT NULL
            GROUP BY c.GstNumber, c.DisplayName, i.InvoiceNumber, i.InvoiceDate,
                     il.GstRate, i.GrandTotal
            ORDER BY i.InvoiceDate, i.InvoiceNumber
            """;
        var rows = await dapper.Connection.QueryAsync<GstR1B2bRow>(sql, new { ShopId = shopId, p.From, p.To });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<HsnSummaryRow>> QueryHsnSummaryAsync(long shopId, DateRangeParams p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                il.HsnSacCodeSnapshot  AS HsnCode,
                il.ProductNameSnapshot AS Description,
                il.UnitCodeSnapshot    AS UomCode,
                SUM(il.QuantityInBaseUnit) AS TotalQuantity,
                SUM(il.TaxableAmount)  AS TaxableValue,
                SUM(il.CgstAmount)     AS CgstAmount,
                SUM(il.SgstAmount)     AS SgstAmount,
                SUM(il.IgstAmount)     AS IgstAmount,
                SUM(il.CgstAmount + il.SgstAmount + il.IgstAmount) AS TotalTax
            FROM sales.InvoiceLine il
            INNER JOIN sales.Invoice i ON i.Id = il.InvoiceId
            WHERE i.ShopId = @ShopId
              AND i.InvoiceDate >= @From
              AND i.InvoiceDate <= @To
              AND i.Status IN ('Finalized','Paid')
              AND il.HsnSacCodeSnapshot IS NOT NULL
            GROUP BY il.HsnSacCodeSnapshot, il.ProductNameSnapshot, il.UnitCodeSnapshot
            ORDER BY il.HsnSacCodeSnapshot
            """;
        var rows = await dapper.Connection.QueryAsync<HsnSummaryRow>(sql, new { ShopId = shopId, p.From, p.To });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<Gstr3bRow>> QueryGstr3bAsync(long shopId, DateRangeParams p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                '3.1(a)' AS Section,
                'Outward taxable supplies (other than zero rated, nil rated and exempted)' AS Description,
                SUM(il.TaxableAmount)   AS TaxableValue,
                SUM(il.CgstAmount)      AS CgstAmount,
                SUM(il.SgstAmount)      AS SgstAmount,
                SUM(il.IgstAmount)      AS IgstAmount,
                SUM(il.CgstAmount + il.SgstAmount + il.IgstAmount) AS TotalTax
            FROM sales.InvoiceLine il
            INNER JOIN sales.Invoice i ON i.Id = il.InvoiceId
            WHERE i.ShopId = @ShopId
              AND i.InvoiceDate >= @From
              AND i.InvoiceDate <= @To
              AND i.Status IN ('Finalized','Paid')
              AND il.GstRate > 0
            UNION ALL
            SELECT
                '4(A)' AS Section,
                'ITC Available - Inputs' AS Description,
                SUM(pol.TaxableAmount)   AS TaxableValue,
                SUM(pol.CgstAmount)      AS CgstAmount,
                SUM(pol.SgstAmount)      AS SgstAmount,
                SUM(pol.IgstAmount)      AS IgstAmount,
                SUM(pol.CgstAmount + pol.SgstAmount + pol.IgstAmount) AS TotalTax
            FROM purchasing.PurchaseOrderLine pol
            INNER JOIN purchasing.PurchaseOrder po ON po.Id = pol.PurchaseOrderId
            INNER JOIN purchasing.Bill b ON b.PurchaseOrderId = po.Id
            WHERE b.ShopId = @ShopId
              AND b.BillDate >= @From
              AND b.BillDate <= @To
              AND b.Status IN ('Approved','PartiallyPaid','Paid')
              AND pol.GstRate > 0
            """;
        var rows = await dapper.Connection.QueryAsync<Gstr3bRow>(sql, new { ShopId = shopId, p.From, p.To });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<CashBookEntry>> QueryCashBookAsync(long shopId, DateRangeParams p, CancellationToken ct = default)
    {
        // Cash accounts are identified by Code '1010' (seeded in AccountingTenantSeeder).
        const string sql = """
            SELECT
                v.VoucherDate   AS Date,
                v.VoucherNumber AS VoucherNumber,
                v.Narration     AS Particulars,
                CASE WHEN ve.[Type] = 'Credit' THEN ve.Amount ELSE 0 END AS Receipts,
                CASE WHEN ve.[Type] = 'Debit'  THEN ve.Amount ELSE 0 END AS Payments,
                SUM(CASE WHEN ve2.[Type] = 'Credit' THEN ve2.Amount ELSE -ve2.Amount END)
                    OVER (PARTITION BY ve.AccountId ORDER BY v.VoucherDate, v.Id ROWS UNBOUNDED PRECEDING) AS RunningBalance
            FROM accounting.VoucherEntry ve
            INNER JOIN accounting.Voucher v ON v.Id = ve.VoucherId
            INNER JOIN accounting.VoucherEntry ve2 ON ve2.VoucherId = ve.VoucherId AND ve2.AccountId = ve.AccountId
            INNER JOIN accounting.Account a ON a.Id = ve.AccountId
            WHERE v.ShopId = @ShopId
              AND v.VoucherDate >= @From
              AND v.VoucherDate <= @To
              AND v.Status = 'Posted'
              AND v.VoucherType IN ('Payment','Receipt','Contra')
              AND a.Code = '1010'
            ORDER BY v.VoucherDate, v.VoucherNumber
            """;
        var rows = await dapper.Connection.QueryAsync<CashBookEntry>(sql, new { ShopId = shopId, p.From, p.To });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<BankBookEntry>> QueryBankBookAsync(long shopId, long bankAccountId, DateRangeParams p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                v.VoucherDate   AS Date,
                v.VoucherNumber AS VoucherNumber,
                v.Narration     AS Particulars,
                ba.BankName     AS BankName,
                CASE WHEN ve.[Type] = 'Credit' THEN ve.Amount ELSE 0 END AS Deposits,
                CASE WHEN ve.[Type] = 'Debit'  THEN ve.Amount ELSE 0 END AS Withdrawals,
                SUM(CASE WHEN ve2.[Type] = 'Credit' THEN ve2.Amount ELSE -ve2.Amount END)
                    OVER (PARTITION BY ve.AccountId ORDER BY v.VoucherDate, v.Id ROWS UNBOUNDED PRECEDING) AS RunningBalance
            FROM accounting.VoucherEntry ve
            INNER JOIN accounting.Voucher v ON v.Id = ve.VoucherId
            INNER JOIN accounting.VoucherEntry ve2 ON ve2.VoucherId = ve.VoucherId AND ve2.AccountId = ve.AccountId
            INNER JOIN accounting.BankAccount ba ON ba.AccountId = ve.AccountId
            WHERE v.ShopId = @ShopId
              AND ba.Id = @BankAccountId
              AND v.VoucherDate >= @From
              AND v.VoucherDate <= @To
              AND v.Status = 'Posted'
              AND v.VoucherType IN ('Payment','Receipt','Contra')
            ORDER BY v.VoucherDate, v.VoucherNumber
            """;
        var rows = await dapper.Connection.QueryAsync<BankBookEntry>(sql, new { ShopId = shopId, BankAccountId = bankAccountId, p.From, p.To });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<WalletStatementEntry>> QueryWalletStatementAsync(long shopId, long customerId, DateRangeParams p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                wt.CreatedAtUtc AS Date,
                wt.TransactionType AS TransactionType,
                wt.Notes AS Particulars,
                CASE WHEN wt.Direction = 'Credit' THEN wt.Amount ELSE 0 END AS Credit,
                CASE WHEN wt.Direction = 'Debit'  THEN wt.Amount ELSE 0 END AS Debit,
                SUM(CASE WHEN wt.Direction = 'Credit' THEN wt.Amount ELSE -wt.Amount END)
                    OVER (PARTITION BY wt.CustomerId ORDER BY wt.CreatedAtUtc, wt.Id ROWS UNBOUNDED PRECEDING) AS RunningBalance
            FROM wallet.WalletTransaction wt
            WHERE wt.ShopId = @ShopId
              AND wt.CustomerId = @CustomerId
              AND wt.CreatedAtUtc >= @From
              AND wt.CreatedAtUtc <= @To
            ORDER BY wt.CreatedAtUtc
            """;
        var rows = await dapper.Connection.QueryAsync<WalletStatementEntry>(sql, new { ShopId = shopId, CustomerId = customerId, p.From, p.To });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<PaymentSummaryRow>> QueryPaymentSummaryAsync(long shopId, DateRangeParams p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                CAST(t.InitiatedAtUtc AS DATE)      AS Date,
                t.GatewayCode,
                t.PaymentMethod                     AS Method,
                COUNT(*)                            AS TotalCount,
                SUM(CASE WHEN t.Status = 'Success'  THEN 1 ELSE 0 END) AS SuccessCount,
                SUM(CASE WHEN t.Status = 'Failed'   THEN 1 ELSE 0 END) AS FailedCount,
                SUM(t.Amount)                       AS TotalAmount,
                SUM(CASE WHEN t.Status = 'Success'  THEN t.Amount ELSE 0 END) AS SuccessAmount,
                CAST(
                    100.0 * SUM(CASE WHEN t.Status = 'Success' THEN 1 ELSE 0 END) / COUNT(*)
                AS DECIMAL(5,2))                    AS SuccessRatePct
            FROM payment.PaymentGatewayTransaction t
            WHERE t.ShopId = @ShopId
              AND t.InitiatedAtUtc >= @From
              AND t.InitiatedAtUtc <= @To
            GROUP BY CAST(t.InitiatedAtUtc AS DATE), t.GatewayCode, t.PaymentMethod
            ORDER BY Date DESC, t.GatewayCode
            """;
        var rows = await dapper.Connection.QueryAsync<PaymentSummaryRow>(sql, new { ShopId = shopId, p.From, p.To });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<FailedPaymentRow>> QueryFailedPaymentsAsync(long shopId, DateRangeParams p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                t.InitiatedAtUtc,
                t.GatewayCode,
                t.OurReferenceNumber,
                t.Amount,
                t.FailureCode,
                t.FailureMessage,
                t.Purpose
            FROM payment.PaymentGatewayTransaction t
            WHERE t.ShopId = @ShopId
              AND t.Status = 'Failed'
              AND t.InitiatedAtUtc >= @From
              AND t.InitiatedAtUtc <= @To
            ORDER BY t.InitiatedAtUtc DESC
            """;
        var rows = await dapper.Connection.QueryAsync<FailedPaymentRow>(sql, new { ShopId = shopId, p.From, p.To });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<SettlementGapRow>> QuerySettlementGapAsync(long shopId, DateRangeParams p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                t.GatewayCode,
                t.GatewayTxnId,
                t.OurReferenceNumber,
                t.Amount,
                ISNULL(t.GatewayFee,   0) AS GatewayFee,
                ISNULL(t.NetSettled,   0) AS NetSettled,
                t.CompletedAtUtc,
                t.SettledAtUtc,
                CASE WHEN t.SettledAtUtc IS NOT NULL
                     THEN DATEDIFF(SECOND, t.CompletedAtUtc, t.SettledAtUtc) / 86400.0
                     ELSE NULL END          AS GapDays
            FROM payment.PaymentGatewayTransaction t
            WHERE t.ShopId = @ShopId
              AND t.Status = 'Success'
              AND t.CompletedAtUtc >= @From
              AND t.CompletedAtUtc <= @To
            ORDER BY GapDays DESC, t.CompletedAtUtc DESC
            """;
        var rows = await dapper.Connection.QueryAsync<SettlementGapRow>(sql, new { ShopId = shopId, p.From, p.To });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ReconciliationExceptionRow>> QueryReconciliationExceptionsAsync(long shopId, DateRangeParams p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                e.DetectedAtUtc,
                e.GatewayCode,
                e.GatewayTxnId,
                e.ExceptionType,
                e.Status,
                e.OurAmount,
                e.GatewayAmount,
                DATEDIFF(SECOND, e.DetectedAtUtc, GETUTCDATE()) / 86400.0 AS AgeDays,
                e.ResolutionNotes
            FROM payment.ReconciliationException e
            WHERE e.ShopId = @ShopId
              AND e.DetectedAtUtc >= @From
              AND e.DetectedAtUtc <= @To
            ORDER BY e.DetectedAtUtc DESC
            """;
        var rows = await dapper.Connection.QueryAsync<ReconciliationExceptionRow>(sql, new { ShopId = shopId, p.From, p.To });
        return rows.ToList();
    }
}
