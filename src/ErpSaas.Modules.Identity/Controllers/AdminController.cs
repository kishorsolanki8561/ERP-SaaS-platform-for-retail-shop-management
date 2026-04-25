using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Identity.Controllers;

[Route("api/admin")]
[Authorize]
public sealed class AdminController(IAdminService adminService) : BaseController
{
    [HttpGet("shop-profile")]
    [RequirePermission("ShopProfile.View")]
    public async Task<IActionResult> GetShopProfile(CancellationToken ct)
    {
        var profile = await adminService.GetShopProfileAsync(ct);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("shop-profile")]
    [RequirePermission("ShopProfile.Edit")]
    public async Task<IActionResult> UpdateShopProfile(
        [FromBody] UpdateShopProfileDto dto, CancellationToken ct)
        => Ok(await adminService.UpdateShopProfileAsync(dto, ct));

    [HttpGet("users")]
    [RequirePermission("Users.View")]
    public async Task<IActionResult> ListUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => Ok(await adminService.ListUsersAsync(pageNumber, pageSize, search, ct));
}
