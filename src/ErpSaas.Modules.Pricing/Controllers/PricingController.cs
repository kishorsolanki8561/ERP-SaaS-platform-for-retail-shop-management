using ErpSaas.Modules.Pricing.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Pricing.Controllers;

[Route("api/pricing")]
[Authorize]
public sealed class PricingController(IPricingManagementService pricingService) : BaseController
{
    [HttpGet("discount-rules")]
    [RequirePermission("Pricing.View")]
    public async Task<IActionResult> ListDiscountRules(CancellationToken ct = default)
        => base.Ok(await pricingService.ListDiscountRulesAsync(ct));

    [HttpPost("discount-rules")]
    [RequirePermission("Pricing.Manage")]
    public async Task<IActionResult> CreateDiscountRule([FromBody] CreateDiscountRuleDto dto, CancellationToken ct = default)
        => Ok(await pricingService.CreateDiscountRuleAsync(dto, ct));

    [HttpPatch("discount-rules/{id:long}/toggle")]
    [RequirePermission("Pricing.Manage")]
    public async Task<IActionResult> ToggleDiscountRule(long id, [FromQuery] bool isActive, CancellationToken ct = default)
        => Ok(await pricingService.ToggleDiscountRuleAsync(id, isActive, ct));

    [HttpPost("extra-charges")]
    [RequirePermission("Pricing.Manage")]
    public async Task<IActionResult> CreateExtraCharge([FromBody] CreateExtraChargeDto dto, CancellationToken ct = default)
        => Ok(await pricingService.CreateExtraChargeAsync(dto, ct));

    [HttpPatch("extra-charges/{id:long}/toggle")]
    [RequirePermission("Pricing.Manage")]
    public async Task<IActionResult> ToggleExtraCharge(long id, [FromQuery] bool isActive, CancellationToken ct = default)
        => Ok(await pricingService.ToggleExtraChargeAsync(id, isActive, ct));

    [HttpPost("offers")]
    [RequirePermission("Pricing.Manage")]
    public async Task<IActionResult> CreateOffer([FromBody] CreateOfferDto dto, CancellationToken ct = default)
        => Ok(await pricingService.CreateOfferAsync(dto, ct));

    [HttpPatch("offers/{id:long}/toggle")]
    [RequirePermission("Pricing.Manage")]
    public async Task<IActionResult> ToggleOffer(long id, [FromQuery] bool isActive, CancellationToken ct = default)
        => Ok(await pricingService.ToggleOfferAsync(id, isActive, ct));

    [HttpPost("calculate")]
    [RequirePermission("Pricing.View")]
    public async Task<IActionResult> Calculate([FromBody] CartInput cart, CancellationToken ct = default)
        => base.Ok(await pricingService.CalculateAsync(cart, ct));
}
