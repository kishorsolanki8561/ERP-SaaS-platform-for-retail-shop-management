using ErpSaas.Modules.Wallet.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using ErpSaas.Shared.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Wallet.Controllers;

[Route("api/wallet")]
[Authorize]
public sealed class WalletController(
    IWalletService walletService,
    IWalletTopUpService topUpService) : BaseController
{
    // ── Balances & transactions ───────────────────────────────────────────────

    [HttpGet("balances")]
    [RequirePermission("Wallet.View")]
    public async Task<IActionResult> ListBalances(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Constants.Pagination.DefaultPageSize,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => Ok(await walletService.ListBalancesAsync(page, pageSize, search, ct));

    [HttpGet("balance/{customerId:long}")]
    [RequirePermission("Wallet.View")]
    public async Task<IActionResult> GetBalance(long customerId, CancellationToken ct = default)
    {
        var result = await walletService.GetBalanceAsync(customerId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("transactions/{customerId:long}")]
    [RequirePermission("Wallet.View")]
    public async Task<IActionResult> ListTransactions(
        long customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Constants.Pagination.DefaultPageSize,
        CancellationToken ct = default)
        => Ok(await walletService.ListTransactionsAsync(customerId, page, pageSize, ct));

    [HttpPost("credit")]
    [RequirePermission("Wallet.Credit")]
    public async Task<IActionResult> Credit(
        [FromBody] WalletCreditDto dto,
        CancellationToken ct = default)
        => Ok(await walletService.CreditAsync(dto, ct));

    [HttpPost("debit")]
    [RequirePermission("Wallet.Debit")]
    public async Task<IActionResult> Debit(
        [FromBody] WalletDebitDto dto,
        CancellationToken ct = default)
        => Ok(await walletService.DebitAsync(dto, ct));

    // ── Top-ups ───────────────────────────────────────────────────────────────

    [HttpPost("top-ups")]
    [RequirePermission("Wallet.TopUp")]
    public async Task<IActionResult> InitiateTopUp(
        [FromBody] InitiateTopUpDto dto,
        CancellationToken ct = default)
        => Ok(await topUpService.InitiateAsync(dto, ct));

    [HttpGet("top-ups/{customerId:long}")]
    [RequirePermission("Wallet.View")]
    public async Task<IActionResult> ListTopUps(
        long customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Constants.Pagination.DefaultPageSize,
        CancellationToken ct = default)
        => Ok(await topUpService.ListAsync(customerId, page, pageSize, ct));

    [HttpGet("top-ups/detail/{id:long}")]
    [RequirePermission("Wallet.View")]
    public async Task<IActionResult> GetTopUp(long id, CancellationToken ct = default)
    {
        var result = await topUpService.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("top-ups/{id:long}/complete")]
    [RequirePermission("Wallet.TopUp")]
    public async Task<IActionResult> CompleteTopUp(
        long id,
        [FromBody] CompleteTopUpDto dto,
        CancellationToken ct = default)
        => Ok(await topUpService.CompleteAsync(id, dto, ct));

    [HttpPost("top-ups/{id:long}/fail")]
    [RequirePermission("Wallet.TopUp")]
    public async Task<IActionResult> FailTopUp(
        long id,
        [FromBody] string reason,
        CancellationToken ct = default)
        => Ok(await topUpService.FailAsync(id, reason, ct));
}
