using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Identity.Controllers;

[Route("api/subscriptions")]
[Authorize]
public sealed class SubscriptionController(ISubscriptionService subscriptionService) : BaseController
{
    [HttpGet("plans")]
    [RequirePermission("Subscription.View")]
    public async Task<IActionResult> ListPlans(CancellationToken ct = default)
    {
        var plans = await subscriptionService.ListPlansAsync(ct);
        return Ok(plans);
    }

    [HttpGet("current")]
    [RequirePermission("Subscription.View")]
    public async Task<IActionResult> GetCurrent(CancellationToken ct = default)
    {
        var sub = await subscriptionService.GetCurrentAsync(ct);
        return sub is null ? NotFound() : Ok(sub);
    }

    [HttpPost("change-plan")]
    [RequirePermission("Subscription.Manage")]
    public async Task<IActionResult> ChangePlan([FromBody] ChangePlanDto dto, CancellationToken ct = default)
    {
        var result = await subscriptionService.ChangePlanAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("cancel")]
    [RequirePermission("Subscription.Manage")]
    public async Task<IActionResult> Cancel(CancellationToken ct = default)
    {
        var result = await subscriptionService.CancelAsync(ct);
        return Ok(result);
    }
}
