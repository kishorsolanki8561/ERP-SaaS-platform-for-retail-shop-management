using ClosedXML.Excel;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Reports.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ErpSaas.Modules.Reports.Services;

public sealed class ReportBuilderService(
    IReportQueryRepository queries,
    ITenantContext tenant,
    IErrorLogger errorLogger,
    ILogger<ReportBuilderService> logger) : IReportBuilderService
{
    public Task<IReadOnlyList<TrialBalanceRow>> GetTrialBalanceAsync(DateRangeParams p, CancellationToken ct = default)
        => queries.QueryTrialBalanceAsync(tenant.ShopId, p, ct);

    public Task<IReadOnlyList<ProfitLossRow>> GetProfitLossAsync(DateRangeParams p, CancellationToken ct = default)
        => queries.QueryProfitLossAsync(tenant.ShopId, p, ct);

    public Task<IReadOnlyList<BalanceSheetRow>> GetBalanceSheetAsync(DateRangeParams p, CancellationToken ct = default)
        => queries.QueryBalanceSheetAsync(tenant.ShopId, p, ct);

    public Task<IReadOnlyList<DayBookEntry>> GetDayBookAsync(DateRangeParams p, CancellationToken ct = default)
        => queries.QueryDayBookAsync(tenant.ShopId, p, ct);

    public Task<IReadOnlyList<LedgerEntry>> GetLedgerAsync(long accountId, DateRangeParams p, CancellationToken ct = default)
        => queries.QueryLedgerAsync(tenant.ShopId, accountId, p, ct);

    public Task<IReadOnlyList<GstR1B2bRow>> GetGstr1B2bAsync(DateRangeParams p, CancellationToken ct = default)
        => queries.QueryGstr1B2bAsync(tenant.ShopId, p, ct);

    public Task<IReadOnlyList<HsnSummaryRow>> GetHsnSummaryAsync(DateRangeParams p, CancellationToken ct = default)
        => queries.QueryHsnSummaryAsync(tenant.ShopId, p, ct);

    public Task<IReadOnlyList<Gstr3bRow>> GetGstr3bAsync(DateRangeParams p, CancellationToken ct = default)
        => queries.QueryGstr3bAsync(tenant.ShopId, p, ct);

    public Task<IReadOnlyList<CashBookEntry>> GetCashBookAsync(DateRangeParams p, CancellationToken ct = default)
        => queries.QueryCashBookAsync(tenant.ShopId, p, ct);

    public Task<IReadOnlyList<BankBookEntry>> GetBankBookAsync(long bankAccountId, DateRangeParams p, CancellationToken ct = default)
        => queries.QueryBankBookAsync(tenant.ShopId, bankAccountId, p, ct);

    public Task<IReadOnlyList<WalletStatementEntry>> GetWalletStatementAsync(long customerId, DateRangeParams p, CancellationToken ct = default)
        => queries.QueryWalletStatementAsync(tenant.ShopId, customerId, p, ct);

    public async Task<Result<ReportFile>> ExportAsync(
        string reportCode, DateRangeParams p, ReportFormat format,
        long? accountId = null, CancellationToken ct = default)
    {
        try
        {
            var (fileName, data) = format switch
            {
                ReportFormat.Pdf   => await BuildPdfAsync(reportCode, p, accountId, ct),
                ReportFormat.Excel => await BuildExcelAsync(reportCode, p, accountId, ct),
                ReportFormat.Csv   => await BuildCsvAsync(reportCode, p, accountId, ct),
                _ => throw new ArgumentOutOfRangeException(nameof(format)),
            };

            var contentType = format switch
            {
                ReportFormat.Pdf   => "application/pdf",
                ReportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ReportFormat.Csv   => "text/csv",
                _ => "application/octet-stream",
            };

            return Result<ReportFile>.Success(new ReportFile(fileName, contentType, data));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Report export failed: {ReportCode}", reportCode);
            await errorLogger.LogAsync("Reports.Export", ex);
            return Result<ReportFile>.Failure("RPT_001");
        }
    }

    private async Task<(string, byte[])> BuildPdfAsync(string reportCode, DateRangeParams p, long? accountId, CancellationToken ct)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var rows = await FetchRowsAsync(reportCode, p, accountId, ct);
        var headers = GetHeaders(reportCode);

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(30);
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        foreach (var _ in headers)
                            cols.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var h in headers)
                            header.Cell().Padding(4).Text(h).Bold();
                    });

                    foreach (var row in rows)
                    {
                        foreach (var cell in row)
                            table.Cell().Padding(4).Text(cell);
                    }
                });
            });
        }).GeneratePdf();

        return ($"{reportCode}_{p.From:yyyyMMdd}_{p.To:yyyyMMdd}.pdf", pdfBytes);
    }

    private async Task<(string, byte[])> BuildExcelAsync(string reportCode, DateRangeParams p, long? accountId, CancellationToken ct)
    {
        var rows = await FetchRowsAsync(reportCode, p, accountId, ct);
        var headers = GetHeaders(reportCode);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(reportCode);
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        for (int r = 0; r < rows.Count; r++)
        {
            for (int c = 0; c < rows[r].Length; c++)
                ws.Cell(r + 2, c + 1).Value = rows[r][c];
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ($"{reportCode}_{p.From:yyyyMMdd}_{p.To:yyyyMMdd}.xlsx", ms.ToArray());
    }

    private async Task<(string, byte[])> BuildCsvAsync(string reportCode, DateRangeParams p, long? accountId, CancellationToken ct)
    {
        var rows = await FetchRowsAsync(reportCode, p, accountId, ct);
        var headers = GetHeaders(reportCode);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(string.Join(",", headers));
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(c => $"\"{c.Replace("\"", "\"\"")}\"")));

        return ($"{reportCode}_{p.From:yyyyMMdd}_{p.To:yyyyMMdd}.csv",
            System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private async Task<List<string[]>> FetchRowsAsync(string reportCode, DateRangeParams p, long? accountId, CancellationToken ct)
    {
        return reportCode switch
        {
            "TrialBalance" => (await GetTrialBalanceAsync(p, ct))
                .Select(r => new[] { r.AccountCode, r.AccountName, r.GroupName,
                    r.Debit.ToString("F2"), r.Credit.ToString("F2"), r.ClosingBalance.ToString("F2") }).ToList(),
            "ProfitLoss" => (await GetProfitLossAsync(p, ct))
                .Select(r => new[] { r.Category, r.AccountName,
                    r.Amount.ToString("F2"), r.IsExpense ? "Expense" : "Income" }).ToList(),
            "BalanceSheet" => (await GetBalanceSheetAsync(p, ct))
                .Select(r => new[] { r.Side, r.GroupName, r.AccountName, r.Amount.ToString("F2") }).ToList(),
            "DayBook" => (await GetDayBookAsync(p, ct))
                .Select(r => new[] { r.Date.ToString("dd/MM/yyyy"), r.VoucherNumber,
                    r.VoucherType, r.Particulars, r.Debit.ToString("F2"), r.Credit.ToString("F2") }).ToList(),
            "Ledger" when accountId.HasValue => (await GetLedgerAsync(accountId.Value, p, ct))
                .Select(r => new[] { r.Date.ToString("dd/MM/yyyy"), r.VoucherNumber,
                    r.Particulars, r.Debit.ToString("F2"), r.Credit.ToString("F2"), r.RunningBalance.ToString("F2") }).ToList(),
            "GstR1B2b" => (await GetGstr1B2bAsync(p, ct))
                .Select(r => new[] { r.CustomerGstin, r.CustomerName, r.InvoiceNumber,
                    r.InvoiceDate.ToString("dd/MM/yyyy"), r.TaxableValue.ToString("F2"),
                    r.CgstAmount.ToString("F2"), r.SgstAmount.ToString("F2"), r.InvoiceValue.ToString("F2") }).ToList(),
            "HsnSummary" => (await GetHsnSummaryAsync(p, ct))
                .Select(r => new[] { r.HsnCode, r.Description, r.UomCode,
                    r.TotalQuantity.ToString("F4"), r.TaxableValue.ToString("F2"),
                    r.CgstAmount.ToString("F2"), r.SgstAmount.ToString("F2"), r.TotalTax.ToString("F2") }).ToList(),
            "Gstr3b" => (await GetGstr3bAsync(p, ct))
                .Select(r => new[] { r.Section, r.Description, r.TaxableValue.ToString("F2"),
                    r.CgstAmount.ToString("F2"), r.SgstAmount.ToString("F2"),
                    r.IgstAmount.ToString("F2"), r.TotalTax.ToString("F2") }).ToList(),
            "CashBook" => (await GetCashBookAsync(p, ct))
                .Select(r => new[] { r.Date.ToString("dd/MM/yyyy"), r.VoucherNumber,
                    r.Particulars, r.Receipts.ToString("F2"), r.Payments.ToString("F2"),
                    r.RunningBalance.ToString("F2") }).ToList(),
            "BankBook" when accountId.HasValue => (await GetBankBookAsync(accountId.Value, p, ct))
                .Select(r => new[] { r.Date.ToString("dd/MM/yyyy"), r.VoucherNumber, r.BankName,
                    r.Particulars, r.Deposits.ToString("F2"), r.Withdrawals.ToString("F2"),
                    r.RunningBalance.ToString("F2") }).ToList(),
            "WalletStatement" when accountId.HasValue => (await GetWalletStatementAsync(accountId.Value, p, ct))
                .Select(r => new[] { r.Date.ToString("dd/MM/yyyy"), r.TransactionType,
                    r.Particulars, r.Credit.ToString("F2"), r.Debit.ToString("F2"),
                    r.RunningBalance.ToString("F2") }).ToList(),
            _ => [],
        };
    }

    private static string[] GetHeaders(string reportCode) => reportCode switch
    {
        "TrialBalance"    => ["Account Code", "Account Name", "Group", "Debit", "Credit", "Closing Balance"],
        "ProfitLoss"      => ["Category", "Account Name", "Amount", "Type"],
        "BalanceSheet"    => ["Side", "Group", "Account Name", "Amount"],
        "DayBook"         => ["Date", "Voucher No", "Type", "Particulars", "Debit", "Credit"],
        "Ledger"          => ["Date", "Voucher No", "Particulars", "Debit", "Credit", "Running Balance"],
        "GstR1B2b"        => ["GSTIN", "Customer", "Invoice No", "Date", "Taxable", "CGST", "SGST", "Total"],
        "HsnSummary"      => ["HSN", "Description", "UOM", "Qty", "Taxable", "CGST", "SGST", "Total Tax"],
        "Gstr3b"          => ["Section", "Description", "Taxable", "CGST", "SGST", "IGST", "Total Tax"],
        "CashBook"        => ["Date", "Voucher No", "Particulars", "Receipts", "Payments", "Balance"],
        "BankBook"        => ["Date", "Voucher No", "Bank", "Particulars", "Deposits", "Withdrawals", "Balance"],
        "WalletStatement" => ["Date", "Type", "Particulars", "Credit", "Debit", "Balance"],
        _ => [],
    };
}
