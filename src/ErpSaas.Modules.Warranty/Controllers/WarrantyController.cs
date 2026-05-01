using ErpSaas.Modules.Warranty.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Warranty.Controllers;

[Route("api/warranty")]
[Authorize]
public sealed class WarrantyController(IWarrantyService warrantyService) : BaseController
{
    [HttpGet("registrations/by-serial/{serial}")]
    [RequirePermission("Warranty.View")]
    public async Task<IActionResult> GetBySerial(string serial, CancellationToken ct = default)
    {
        var result = await warrantyService.GetBySerialAsync(serial, ct);
        return result is null ? NotFound() : base.Ok(result);
    }

    [HttpGet("registrations/expiring")]
    [RequirePermission("Warranty.View")]
    public async Task<IActionResult> ListExpiring([FromQuery] int days = 30, CancellationToken ct = default)
        => base.Ok(await warrantyService.ListExpiringAsync(days, ct));

    [HttpGet("registrations/customer/{customerId:long}")]
    [RequirePermission("Warranty.View")]
    public async Task<IActionResult> ListByCustomer(long customerId, CancellationToken ct = default)
        => base.Ok(await warrantyService.ListByCustomerAsync(customerId, ct));

    [HttpPost("registrations")]
    [RequirePermission("Warranty.Manage")]
    public async Task<IActionResult> RegisterWarranty([FromBody] RegisterWarrantyDto dto, CancellationToken ct = default)
        => Ok(await warrantyService.RegisterWarrantyAsync(dto, ct));

    [HttpGet("claims")]
    [RequirePermission("Warranty.View")]
    public async Task<IActionResult> ListClaims(CancellationToken ct = default)
        => base.Ok(await warrantyService.ListClaimsAsync(ct));

    [HttpPost("claims")]
    [RequirePermission("Warranty.ManageClaims")]
    public async Task<IActionResult> CreateClaim([FromBody] CreateClaimDto dto, CancellationToken ct = default)
        => Ok(await warrantyService.CreateClaimAsync(dto, ct));

    [HttpPatch("claims/{id:long}")]
    [RequirePermission("Warranty.ManageClaims")]
    public async Task<IActionResult> ResolveClaim(long id, [FromBody] ResolveClaimDto dto, CancellationToken ct = default)
        => Ok(await warrantyService.ResolveClaimAsync(id, dto, ct));
}
