using ErpSaas.Modules.Reports.Enums;
using ErpSaas.Modules.Reports.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Reports.Controllers;

[Route("api/reports")]
[Authorize]
public sealed class ReportsController(IReportBuilderService reportBuilder) : BaseController
{
    [HttpGet("trial-balance")]
    [RequirePermission("Reports.ViewAccounting")]
    public async Task<IActionResult> TrialBalance(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
        => Ok(await reportBuilder.GetTrialBalanceAsync(new(from, to), ct));

    [HttpGet("profit-loss")]
    [RequirePermission("Reports.ViewAccounting")]
    public async Task<IActionResult> ProfitLoss(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
        => Ok(await reportBuilder.GetProfitLossAsync(new(from, to), ct));

    [HttpGet("balance-sheet")]
    [RequirePermission("Reports.ViewAccounting")]
    public async Task<IActionResult> BalanceSheet(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
        => Ok(await reportBuilder.GetBalanceSheetAsync(new(from, to), ct));

    [HttpGet("day-book")]
    [RequirePermission("Reports.ViewAccounting")]
    public async Task<IActionResult> DayBook(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
        => Ok(await reportBuilder.GetDayBookAsync(new(from, to), ct));

    [HttpGet("ledger/{accountId:long}")]
    [RequirePermission("Reports.ViewAccounting")]
    public async Task<IActionResult> Ledger(
        long accountId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
        => Ok(await reportBuilder.GetLedgerAsync(accountId, new(from, to), ct));

    [HttpGet("gstr1-b2b")]
    [RequirePermission("Reports.ViewGst")]
    public async Task<IActionResult> Gstr1B2b(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
        => Ok(await reportBuilder.GetGstr1B2bAsync(new(from, to), ct));

    [HttpGet("hsn-summary")]
    [RequirePermission("Reports.ViewGst")]
    public async Task<IActionResult> HsnSummary(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
        => Ok(await reportBuilder.GetHsnSummaryAsync(new(from, to), ct));

    [HttpGet("gstr3b")]
    [RequirePermission("Reports.ViewGst")]
    [RequireFeature("Accounting.GstReturns")]
    public async Task<IActionResult> Gstr3b(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
        => Ok(await reportBuilder.GetGstr3bAsync(new(from, to), ct));

    [HttpGet("cash-book")]
    [RequirePermission("Reports.ViewAccounting")]
    public async Task<IActionResult> CashBook(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
        => Ok(await reportBuilder.GetCashBookAsync(new(from, to), ct));

    [HttpGet("bank-book/{bankAccountId:long}")]
    [RequirePermission("Reports.ViewAccounting")]
    public async Task<IActionResult> BankBook(
        long bankAccountId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
        => Ok(await reportBuilder.GetBankBookAsync(bankAccountId, new(from, to), ct));

    [HttpGet("wallet-statement/{customerId:long}")]
    [RequirePermission("Reports.ViewSales")]
    public async Task<IActionResult> WalletStatement(
        long customerId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
        => Ok(await reportBuilder.GetWalletStatementAsync(customerId, new(from, to), ct));

    [HttpGet("payments/summary")]
    [RequirePermission("Reports.ViewPayment")]
    public async Task<IActionResult> PaymentSummary(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
        => Ok(await reportBuilder.GetPaymentSummaryAsync(new(from, to), ct));

    [HttpGet("payments/failed")]
    [RequirePermission("Reports.ViewPayment")]
    public async Task<IActionResult> FailedPayments(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
        => Ok(await reportBuilder.GetFailedPaymentsAsync(new(from, to), ct));

    [HttpGet("payments/settlement")]
    [RequirePermission("Reports.ViewPayment")]
    public async Task<IActionResult> SettlementGap(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
        => Ok(await reportBuilder.GetSettlementGapAsync(new(from, to), ct));

    [HttpGet("payments/reconciliation-exceptions")]
    [RequirePermission("Reports.ViewPayment")]
    public async Task<IActionResult> ReconciliationExceptions(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
        => Ok(await reportBuilder.GetReconciliationExceptionsAsync(new(from, to), ct));

    [HttpGet("export/{reportCode}")]
    [RequirePermission("Reports.Export")]
    public async Task<IActionResult> Export(
        string reportCode,
        [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] ReportFormat format = ReportFormat.Excel,
        [FromQuery] long? accountId = null,
        CancellationToken ct = default)
    {
        var result = await reportBuilder.ExportAsync(reportCode, new(from, to), format, accountId, ct);
        if (!result.IsSuccess)
            return Ok(result);

        var file = result.Value!;
        return File(file.Data, file.ContentType, file.FileName);
    }
}
