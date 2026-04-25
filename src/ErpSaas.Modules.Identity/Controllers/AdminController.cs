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

    [HttpPost("users/invite")]
    [RequirePermission("Users.Invite")]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserDto dto, CancellationToken ct)
        => Ok(await adminService.InviteUserAsync(dto, ct));

    [HttpPost("users/{userId:long}/reinvite")]
    [RequirePermission("Users.Invite")]
    public async Task<IActionResult> ResendInvite(long userId, CancellationToken ct)
        => Ok(await adminService.ResendInviteAsync(userId, ct));

    [HttpPost("users/{userId:long}/force-reset")]
    [RequirePermission("Users.Manage")]
    public async Task<IActionResult> ForceResetPassword(long userId, CancellationToken ct)
        => Ok(await adminService.ForceResetPasswordAsync(userId, ct));

    [HttpPost("users/{userId:long}/unlock")]
    [RequirePermission("Users.Manage")]
    public async Task<IActionResult> UnlockUser(long userId, CancellationToken ct)
        => Ok(await adminService.UnlockUserAsync(userId, ct));

    [HttpDelete("users/{userId:long}")]
    [RequirePermission("Users.Manage")]
    public async Task<IActionResult> DeactivateUser(long userId, CancellationToken ct)
        => Ok(await adminService.DeactivateUserAsync(userId, ct));

    [HttpGet("permissions")]
    [RequirePermission("Users.View")]
    public async Task<IActionResult> ListPermissions(CancellationToken ct)
        => Ok(await adminService.ListPermissionsAsync(ct));

    [HttpGet("roles")]
    [RequirePermission("Users.View")]
    public async Task<IActionResult> ListRoles(CancellationToken ct)
        => Ok(await adminService.ListRolesAsync(ct));

    [HttpPost("roles")]
    [RequirePermission("Users.Manage")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto, CancellationToken ct)
        => Ok(await adminService.CreateRoleAsync(dto, ct));

    [HttpPatch("roles/{roleId:long}/permissions")]
    [RequirePermission("Users.Manage")]
    public async Task<IActionResult> UpdateRolePermissions(
        long roleId,
        [FromBody] UpdateRolePermissionsDto dto,
        CancellationToken ct)
        => Ok(await adminService.UpdateRolePermissionsAsync(roleId, dto, ct));

    [HttpPost("users/{userId:long}/roles/{roleId:long}")]
    [RequirePermission("Users.Manage")]
    public async Task<IActionResult> AssignUserRole(long userId, long roleId, CancellationToken ct)
        => Ok(await adminService.AssignUserRoleAsync(userId, roleId, ct));

    [HttpDelete("users/{userId:long}/roles/{roleId:long}")]
    [RequirePermission("Users.Manage")]
    public async Task<IActionResult> RemoveUserRole(long userId, long roleId, CancellationToken ct)
        => Ok(await adminService.RemoveUserRoleAsync(userId, roleId, ct));

    // ── Branches ──────────────────────────────────────────────────────────────

    [HttpGet("branches")]
    [RequirePermission("ShopProfile.View")]
    public async Task<IActionResult> ListBranches(CancellationToken ct)
        => Ok(await adminService.ListBranchesAsync(ct));

    [HttpPost("branches")]
    [RequirePermission("ShopProfile.Edit")]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchDto dto, CancellationToken ct)
        => Ok(await adminService.CreateBranchAsync(dto, ct));

    [HttpPut("branches/{branchId:long}")]
    [RequirePermission("ShopProfile.Edit")]
    public async Task<IActionResult> UpdateBranch(long branchId, [FromBody] UpdateBranchDto dto, CancellationToken ct)
        => Ok(await adminService.UpdateBranchAsync(branchId, dto, ct));

    [HttpDelete("branches/{branchId:long}")]
    [RequirePermission("ShopProfile.Edit")]
    public async Task<IActionResult> DeactivateBranch(long branchId, CancellationToken ct)
        => Ok(await adminService.DeactivateBranchAsync(branchId, ct));
}
