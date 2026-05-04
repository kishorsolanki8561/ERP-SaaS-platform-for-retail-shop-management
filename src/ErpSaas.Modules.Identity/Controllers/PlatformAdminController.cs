using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Identity.Controllers;

[Route("api/platform")]
[Authorize]
public sealed class PlatformAdminController(
    IPlatformAdminService platformAdminService,
    IAuditLogService auditLogService) : BaseController
{
    // ── Existing shop endpoints ───────────────────────────────────────────────

    [HttpGet("shops")]
    [RequirePermission("Platform.Shops.View")]
    public async Task<IActionResult> ListShops(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var (items, total) = await platformAdminService.ListShopsAsync(pageNumber, pageSize, search, ct);
        return Ok(new { items, totalCount = total, pageNumber, pageSize });
    }

    [HttpGet("shops/{shopId:long}")]
    [RequirePermission("Platform.Shops.View")]
    public async Task<IActionResult> GetShop(long shopId, CancellationToken ct)
    {
        var detail = await platformAdminService.GetShopDetailAsync(shopId, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("shops/{shopId:long}/users")]
    [RequirePermission("Platform.Shops.View")]
    public async Task<IActionResult> ListShopUsers(long shopId, CancellationToken ct)
    {
        var users = await platformAdminService.ListShopUsersAsync(shopId, ct);
        return Ok(users);
    }

    [HttpGet("shops/{shopId:long}/audit-logs")]
    [RequirePermission("Platform.Shops.View")]
    public async Task<IActionResult> GetShopAuditLogs(
        long shopId,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await auditLogService.ListAsync(
            entityType ?? "", entityId, from, to,
            shopId, pageNumber, pageSize, ct);

        return Ok(result);
    }

    // ── Part 3: Subscription dashboard ───────────────────────────────────────

    [HttpGet("subscription-dashboard")]
    [RequirePermission("Platform.Shops.View")]
    public async Task<IActionResult> GetSubscriptionDashboard(CancellationToken ct)
    {
        var result = await platformAdminService.GetSubscriptionDashboardAsync(ct);
        return Ok(result);
    }

    // ── Part 3: System health ─────────────────────────────────────────────────

    [HttpGet("health")]
    [RequirePermission("Platform.Shops.View")]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        var result = await platformAdminService.GetSystemHealthAsync(ct);
        return Ok(result);
    }

    // ── Part 3: Plan CRUD ─────────────────────────────────────────────────────

    [HttpGet("plans")]
    [RequirePermission("Platform.Shops.Manage")]
    public async Task<IActionResult> ListPlans(CancellationToken ct)
    {
        var plans = await platformAdminService.ListPlansAsync(ct);
        return Ok(plans);
    }

    [HttpPost("plans")]
    [RequirePermission("Platform.Shops.Manage")]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanDto dto, CancellationToken ct)
    {
        var result = await platformAdminService.CreatePlanAsync(dto, ct);
        return Ok(result);
    }

    [HttpPut("plans/{planId:long}")]
    [RequirePermission("Platform.Shops.Manage")]
    public async Task<IActionResult> UpdatePlan(long planId, [FromBody] UpdatePlanDto dto, CancellationToken ct)
    {
        var result = await platformAdminService.UpdatePlanAsync(planId, dto, ct);
        return Ok(result);
    }

    // ── Part 3: Shop actions ──────────────────────────────────────────────────

    [HttpPost("shops/{shopId:long}/features")]
    [RequirePermission("Platform.Shops.Manage")]
    public async Task<IActionResult> ToggleShopFeature(long shopId, [FromBody] ToggleFeatureDto dto, CancellationToken ct)
    {
        var result = await platformAdminService.ToggleShopFeatureAsync(shopId, dto, ct);
        return Ok(result);
    }

    [HttpPost("shops/{shopId:long}/suspend")]
    [RequirePermission("Platform.Shops.Manage")]
    public async Task<IActionResult> SuspendShop(long shopId, [FromBody] SuspendShopDto dto, CancellationToken ct)
    {
        var result = await platformAdminService.SuspendShopAsync(shopId, dto, ct);
        return Ok(result);
    }

    [HttpPost("shops/{shopId:long}/activate")]
    [RequirePermission("Platform.Shops.Manage")]
    public async Task<IActionResult> ActivateShop(long shopId, CancellationToken ct)
    {
        var result = await platformAdminService.ActivateShopAsync(shopId, ct);
        return Ok(result);
    }
}
