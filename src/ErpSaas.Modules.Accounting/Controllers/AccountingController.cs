using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Modules.Accounting.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using ErpSaas.Shared.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Accounting.Controllers;

[Route("api/accounting")]
[Authorize]
public sealed class AccountingController(
    IAccountingService accountingService,
    IBankReconciliationService bankReconciliation,
    IChequeService chequeService,
    IPettyCashService pettyCashService,
    ITenantContext tenant) : BaseController
{
    // ── Account Groups ────────────────────────────────────────────────────────

    [HttpGet("account-groups")]
    [RequirePermission("Accounting.View")]
    public async Task<IActionResult> ListAccountGroups(CancellationToken ct = default)
        => Ok(await accountingService.ListAccountGroupsAsync(ct));

    // ── Accounts ──────────────────────────────────────────────────────────────

    [HttpGet("accounts")]
    [RequirePermission("Accounting.View")]
    public async Task<IActionResult> ListAccounts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => Ok(await accountingService.ListAccountsAsync(page, pageSize, search, ct));

    [HttpGet("accounts/{id:long}")]
    [RequirePermission("Accounting.View")]
    public async Task<IActionResult> GetAccount(long id, CancellationToken ct = default)
    {
        var result = await accountingService.GetAccountAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("accounts")]
    [RequirePermission("Accounting.ManageAccounts")]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountDto dto,
        CancellationToken ct = default)
        => Ok(await accountingService.CreateAccountAsync(dto, ct));

    [HttpPatch("accounts/{id:long}")]
    [RequirePermission("Accounting.ManageAccounts")]
    public async Task<IActionResult> UpdateAccount(
        long id,
        [FromBody] UpdateAccountDto dto,
        CancellationToken ct = default)
        => Ok(await accountingService.UpdateAccountAsync(id, dto, ct));

    // ── Vouchers ──────────────────────────────────────────────────────────────

    [HttpGet("vouchers")]
    [RequirePermission("Accounting.View")]
    public async Task<IActionResult> ListVouchers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] VoucherType? type = null,
        CancellationToken ct = default)
        => Ok(await accountingService.ListVouchersAsync(page, pageSize, type, ct));

    [HttpGet("vouchers/{id:long}")]
    [RequirePermission("Accounting.View")]
    public async Task<IActionResult> GetVoucher(long id, CancellationToken ct = default)
    {
        var result = await accountingService.GetVoucherAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("vouchers")]
    [RequirePermission("Accounting.CreateVoucher")]
    public async Task<IActionResult> CreateVoucher(
        [FromBody] CreateVoucherDto dto,
        CancellationToken ct = default)
        => Ok(await accountingService.CreateVoucherAsync(dto, ct));

    [HttpPost("vouchers/{id:long}/post")]
    [RequirePermission("Accounting.PostVoucher")]
    public async Task<IActionResult> PostVoucher(long id, CancellationToken ct = default)
        => Ok(await accountingService.PostVoucherAsync(id, ct));

    [HttpPost("vouchers/{id:long}/reverse")]
    [RequirePermission("Accounting.PostVoucher")]
    public async Task<IActionResult> ReverseVoucher(
        long id,
        [FromBody] ReverseVoucherRequest request,
        CancellationToken ct = default)
        => Ok(await accountingService.ReverseVoucherAsync(id, request.Narration, ct));

    // ── Expenses ──────────────────────────────────────────────────────────────

    [HttpGet("expenses")]
    [RequirePermission("Accounting.View")]
    public async Task<IActionResult> ListExpenses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await accountingService.ListExpensesAsync(page, pageSize, ct));

    [HttpPost("expenses")]
    [RequirePermission("Accounting.ManageExpenses")]
    public async Task<IActionResult> CreateExpense(
        [FromBody] CreateExpenseDto dto,
        CancellationToken ct = default)
        => Ok(await accountingService.CreateExpenseAsync(dto, ct));

    // ── Bank Accounts ─────────────────────────────────────────────────────────

    [HttpGet("bank-accounts")]
    [RequirePermission("Accounting.View")]
    public async Task<IActionResult> ListBankAccounts(CancellationToken ct = default)
        => Ok(await accountingService.ListBankAccountsAsync(ct));

    [HttpPost("bank-accounts")]
    [RequirePermission("Accounting.ManageAccounts")]
    public async Task<IActionResult> CreateBankAccount(
        [FromBody] CreateBankAccountDto dto,
        CancellationToken ct = default)
        => Ok(await accountingService.CreateBankAccountAsync(dto, ct));

    // ── Financial Years ───────────────────────────────────────────────────────

    [HttpGet("financial-years")]
    [RequirePermission("Accounting.View")]
    public async Task<IActionResult> ListFinancialYears(CancellationToken ct = default)
        => Ok(await accountingService.ListFinancialYearsAsync(ct));

    [HttpPost("financial-years")]
    [RequirePermission("Accounting.ManageAccounts")]
    public async Task<IActionResult> CreateFinancialYear(
        [FromBody] CreateFinancialYearDto dto,
        CancellationToken ct = default)
        => Ok(await accountingService.CreateFinancialYearAsync(dto, ct));

    [HttpPost("financial-years/{id:long}/close")]
    [RequirePermission("Accounting.CloseFinancialYear")]
    [RequireFeature("Accounting.Basic")]
    public async Task<IActionResult> CloseFinancialYear(long id, CancellationToken ct = default)
        => Ok(await accountingService.CloseFinancialYearAsync(id, ct));

    // ── Bank Reconciliation ───────────────────────────────────────────────────

    [HttpGet("bank-statements")]
    [RequirePermission("Accounting.BankReconciliation")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> ListBankStatements(
        [FromQuery] long? bankAccountId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await bankReconciliation.ListBankStatementsAsync(bankAccountId, page, pageSize, ct));

    [HttpPost("bank-statements")]
    [RequirePermission("Accounting.BankReconciliation")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> CreateBankStatement(
        [FromBody] CreateBankStatementDto dto,
        CancellationToken ct = default)
        => Ok(await bankReconciliation.CreateBankStatementAsync(dto, ct));

    [HttpGet("bank-statements/{id:long}")]
    [RequirePermission("Accounting.BankReconciliation")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> GetBankStatement(long id, CancellationToken ct = default)
    {
        var result = await bankReconciliation.GetBankStatementAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("bank-statements/{id:long}/lines/import")]
    [RequirePermission("Accounting.BankReconciliation")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> ImportBankStatementLines(
        long id,
        [FromBody] IReadOnlyList<ImportBankStatementLineDto> lines,
        CancellationToken ct = default)
        => Ok(await bankReconciliation.ImportLinesAsync(id, lines, ct));

    [HttpPost("bank-statements/{id:long}/auto-match")]
    [RequirePermission("Accounting.BankReconciliation")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> AutoMatch(long id, CancellationToken ct = default)
        => Ok(await bankReconciliation.AutoMatchAsync(id, ct));

    [HttpPost("bank-statement-lines/{lineId:long}/match")]
    [RequirePermission("Accounting.BankReconciliation")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> ManualMatchLine(
        long lineId,
        [FromBody] ManualMatchLineDto dto,
        CancellationToken ct = default)
        => Ok(await bankReconciliation.ManualMatchLineAsync(lineId, dto, ct));

    [HttpPost("bank-statement-lines/{lineId:long}/ignore")]
    [RequirePermission("Accounting.BankReconciliation")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> IgnoreLine(long lineId, CancellationToken ct = default)
        => Ok(await bankReconciliation.IgnoreLineAsync(lineId, ct));

    [HttpPost("bank-statement-lines/adjustment")]
    [RequirePermission("Accounting.BankReconciliation")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> PostAdjustment(
        [FromBody] PostAdjustmentDto dto,
        CancellationToken ct = default)
        => Ok(await bankReconciliation.PostAdjustmentAsync(dto, ct));

    [HttpPost("bank-statements/{id:long}/complete")]
    [RequirePermission("Accounting.BankReconciliation")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> CompleteReconciliation(long id, CancellationToken ct = default)
        => Ok(await bankReconciliation.CompleteReconciliationAsync(id, ct));

    [HttpGet("reconciliation-rules")]
    [RequirePermission("Accounting.BankReconciliation")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> ListReconciliationRules(CancellationToken ct = default)
        => Ok(await bankReconciliation.ListReconciliationRulesAsync(ct));

    [HttpPost("reconciliation-rules")]
    [RequirePermission("Accounting.BankReconciliation")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> CreateReconciliationRule(
        [FromBody] CreateReconciliationRuleDto dto,
        CancellationToken ct = default)
        => Ok(await bankReconciliation.CreateReconciliationRuleAsync(dto, ct));

    [HttpPost("reconciliation-rules/{ruleId:long}/toggle")]
    [RequirePermission("Accounting.BankReconciliation")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> ToggleReconciliationRule(long ruleId, CancellationToken ct = default)
        => Ok(await bankReconciliation.ToggleReconciliationRuleAsync(ruleId, ct));

    // ── Cheques ───────────────────────────────────────────────────────────────

    [HttpGet("cheques")]
    [RequirePermission("Accounting.ManageCheques")]
    public async Task<IActionResult> ListCheques(
        [FromQuery] ChequeStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await chequeService.ListChequesAsync(status, page, pageSize, ct));

    [HttpPost("cheques")]
    [RequirePermission("Accounting.ManageCheques")]
    public async Task<IActionResult> ReceiveCheque(
        [FromBody] ReceiveChequeDto dto,
        CancellationToken ct = default)
        => Ok(await chequeService.ReceiveChequeAsync(dto, ct));

    [HttpPost("cheques/{id:long}/deposit")]
    [RequirePermission("Accounting.ManageCheques")]
    public async Task<IActionResult> DepositCheque(
        long id,
        [FromBody] DepositChequeRequest request,
        CancellationToken ct = default)
        => Ok(await chequeService.DepositChequeAsync(id, request.DepositedDate, ct));

    [HttpPost("cheques/{id:long}/clear")]
    [RequirePermission("Accounting.ManageCheques")]
    public async Task<IActionResult> ClearCheque(
        long id,
        [FromBody] ClearChequeRequest request,
        CancellationToken ct = default)
        => Ok(await chequeService.ClearChequeAsync(id, request.ClearedDate, ct));

    [HttpPost("cheques/{id:long}/bounce")]
    [RequirePermission("Accounting.ManageCheques")]
    public async Task<IActionResult> BounceCheque(
        long id,
        [FromBody] BounceChequeDtoRequest dto,
        CancellationToken ct = default)
        => Ok(await chequeService.BounceChequeAsync(id, dto, ct));

    [HttpPost("cheques/{id:long}/cancel")]
    [RequirePermission("Accounting.ManageCheques")]
    public async Task<IActionResult> CancelCheque(long id, CancellationToken ct = default)
        => Ok(await chequeService.CancelChequeAsync(id, ct));

    // ── Petty Cash ────────────────────────────────────────────────────────────

    [HttpPost("petty-cash/top-up")]
    [RequirePermission("Accounting.ManageExpenses")]
    public async Task<IActionResult> PettyCashTopUp(
        [FromBody] PettyCashTopUpDto dto,
        CancellationToken ct = default)
        => Ok(await pettyCashService.TopUpAsync(dto, ct));

    [HttpPost("petty-cash/expense")]
    [RequirePermission("Accounting.ManageExpenses")]
    public async Task<IActionResult> PettyCashExpense(
        [FromBody] PettyCashExpenseDto dto,
        CancellationToken ct = default)
        => Ok(await pettyCashService.RecordExpenseAsync(dto, ct));

    [HttpPost("petty-cash/close")]
    [RequirePermission("Accounting.ManageExpenses")]
    public async Task<IActionResult> PettyCashClose(
        [FromBody] PettyCashClosureDto dto,
        CancellationToken ct = default)
        => Ok(await pettyCashService.ClosePeriodAsync(dto, ct));

    [HttpGet("petty-cash/closures")]
    [RequirePermission("Accounting.View")]
    public async Task<IActionResult> ListPettyCashClosures(CancellationToken ct = default)
        => Ok(await pettyCashService.ListClosuresAsync(ct));
}

public record ReverseVoucherRequest(string Narration);
public record DepositChequeRequest(DateTime DepositedDate);
public record ClearChequeRequest(DateTime ClearedDate);
