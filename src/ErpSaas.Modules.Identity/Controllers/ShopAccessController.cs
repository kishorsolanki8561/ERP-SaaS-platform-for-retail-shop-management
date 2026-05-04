using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using ErpSaas.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Identity.Controllers;

[Route("api/shop/access")]
[Authorize]
public sealed class ShopAccessController(IShopAccessService shopAccessService) : BaseController
{
    [HttpGet("modules")]
    [RequirePermission("ShopProfile.View")]
    public async Task<IActionResult> GetModuleAccess(CancellationToken ct)
        => Ok(await shopAccessService.GetModuleAccessAsync(ct));

    [HttpPut("modules/{featureCode}")]
    [RequirePermission("Admin.ManageAccess")]
    public async Task<IActionResult> SetModuleVisibility(
        string featureCode,
        [FromBody] SetModuleVisibilityRequest request,
        CancellationToken ct)
    {
        var result = await shopAccessService.SetModuleVisibilityAsync(
            new SetModuleVisibilityDto(featureCode, request.IsVisible), ct);
        return result.ToActionResult();
    }

    [HttpGet("users/{userId:long}/permissions")]
    [RequirePermission("Users.View")]
    public async Task<IActionResult> GetUserPermissions(long userId, CancellationToken ct)
    {
        var result = await shopAccessService.GetUserPermissionsAsync(userId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("users/{userId:long}/permissions")]
    [RequirePermission("Users.Manage")]
    public async Task<IActionResult> SetUserPermissionOverride(
        long userId,
        [FromBody] SetPermissionOverrideDto dto,
        CancellationToken ct)
    {
        var result = await shopAccessService.SetUserPermissionOverrideAsync(userId, dto, ct);
        return result.ToActionResult();
    }

    [HttpDelete("users/{userId:long}/permissions/{code}")]
    [RequirePermission("Users.Manage")]
    public async Task<IActionResult> RemoveUserPermissionOverride(
        long userId,
        string code,
        CancellationToken ct)
    {
        var result = await shopAccessService.RemoveUserPermissionOverrideAsync(userId, code, ct);
        return result.ToActionResult();
    }
}

public sealed record SetModuleVisibilityRequest(bool IsVisible);
