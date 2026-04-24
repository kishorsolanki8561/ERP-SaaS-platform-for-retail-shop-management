using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Identity.Controllers;

[Route("api/menu")]
[Authorize]
public sealed class MenuController(IMenuService menuService) : BaseController
{
    [HttpGet("tree")]
    public async Task<IActionResult> Tree(CancellationToken ct)
    {
        var tree = await menuService.GetTreeAsync(CurrentUserId, CurrentShopId, ct);
        return Ok(tree);
    }
}
