using ErpSaas.Modules.Wallet.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using ErpSaas.Shared.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Wallet.Controllers;

[Route("api/wallet")]
[Authorize]
public sealed class WalletController(IWalletService walletService) : BaseController
{
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
}
