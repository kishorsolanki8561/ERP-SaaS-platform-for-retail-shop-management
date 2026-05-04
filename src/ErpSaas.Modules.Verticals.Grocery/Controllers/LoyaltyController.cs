using ErpSaas.Modules.Verticals.Grocery.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Verticals.Grocery.Controllers;

[Route("api/loyalty")]
[Authorize]
[RequireFeature("Verticals.GroceryLoyaltyPoints")]
public sealed class LoyaltyController(ILoyaltyService loyaltyService) : BaseController
{
    [HttpGet("program")]
    [RequirePermission("Loyalty.View")]
    public async Task<IActionResult> GetProgram(CancellationToken ct = default)
        => Ok(await loyaltyService.GetProgramAsync(ct));

    [HttpPost("program")]
    [RequirePermission("Loyalty.Manage")]
    public async Task<IActionResult> UpsertProgram([FromBody] LoyaltyProgramDto dto, CancellationToken ct = default)
        => Ok(await loyaltyService.CreateOrUpdateProgramAsync(dto, ct));

    [HttpGet("customers/{customerId:long}/balance")]
    [RequirePermission("Loyalty.View")]
    public async Task<IActionResult> GetBalance(long customerId, CancellationToken ct = default)
        => Ok(await loyaltyService.GetCustomerBalanceAsync(customerId, ct));

    [HttpGet("customers/{customerId:long}/history")]
    [RequirePermission("Loyalty.View")]
    public async Task<IActionResult> GetHistory(long customerId, CancellationToken ct = default)
        => Ok(await loyaltyService.GetCustomerHistoryAsync(customerId, ct));

    [HttpPost("earn")]
    [RequirePermission("Loyalty.Earn")]
    public async Task<IActionResult> Earn([FromBody] EarnPointsDto dto, CancellationToken ct = default)
        => Ok(await loyaltyService.EarnPointsAsync(dto, ct));

    [HttpPost("redeem")]
    [RequirePermission("Loyalty.Redeem")]
    public async Task<IActionResult> Redeem([FromBody] RedeemPointsDto dto, CancellationToken ct = default)
        => Ok(await loyaltyService.RedeemPointsAsync(dto, ct));
}
