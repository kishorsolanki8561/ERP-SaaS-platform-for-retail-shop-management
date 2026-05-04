using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Identity.Controllers;

[Route("api")]
public sealed class ShopRegistrationController(IShopRegistrationService registrationService) : BaseController
{
    // ── Public ────────────────────────────────────────────────────────────────

    [HttpPost("shop-registration")]
    [AllowAnonymous]
    [RequireCaptcha]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitRegistrationHttpRequest request, CancellationToken ct)
        => Ok(await registrationService.SubmitAsync(
            new SubmitRegistrationRequest(
                request.ShopCode,
                request.LegalName,
                request.AdminEmail,
                request.AdminDisplayName,
                request.Password,
                request.TradeName,
                request.GstNumber,
                request.ContactPhone,
                request.Notes), ct));

    // ── Platform Admin ────────────────────────────────────────────────────────

    [HttpGet("platform/shop-registrations")]
    [Authorize]
    [RequirePermission("Platform.Registrations.View")]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] RegistrationStatus? status = null,
        CancellationToken ct = default)
    {
        var (items, total) = await registrationService.ListAsync(page, pageSize, status, ct);
        return Ok(new { items, totalCount = total });
    }

    [HttpGet("platform/shop-registrations/{id:long}")]
    [Authorize]
    [RequirePermission("Platform.Registrations.View")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var reg = await registrationService.GetAsync(id, ct);
        return reg is null ? NotFound() : Ok(reg);
    }

    [HttpPost("platform/shop-registrations/{id:long}/approve")]
    [Authorize]
    [RequirePermission("Platform.Registrations.Manage")]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
        => Ok(await registrationService.ApproveAsync(id, CurrentUserId, ct));

    [HttpPost("platform/shop-registrations/{id:long}/reject")]
    [Authorize]
    [RequirePermission("Platform.Registrations.Manage")]
    public async Task<IActionResult> Reject(
        long id, [FromBody] RejectRegistrationRequest request, CancellationToken ct)
        => Ok(await registrationService.RejectAsync(id, request.Reason, CurrentUserId, ct));
}

public record SubmitRegistrationHttpRequest(
    string ShopCode,
    string LegalName,
    string AdminEmail,
    string AdminDisplayName,
    string Password,
    string? TradeName = null,
    string? GstNumber = null,
    string? ContactPhone = null,
    string? Notes = null);

public record RejectRegistrationRequest(string Reason);
