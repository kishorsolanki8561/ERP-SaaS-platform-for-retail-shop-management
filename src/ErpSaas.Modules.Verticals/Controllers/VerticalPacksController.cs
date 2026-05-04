using ErpSaas.Modules.Verticals.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Verticals.Controllers;

[Route("api/verticals")]
[Authorize]
public sealed class VerticalPacksController(IVerticalPackService verticalPackService) : BaseController
{
    [HttpGet("packs")]
    [AllowAnonymous]
    public async Task<IActionResult> ListPacks(CancellationToken ct = default)
        => Ok(await verticalPackService.ListPacksAsync(ct));

    [HttpGet("packs/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPack(string code, CancellationToken ct = default)
    {
        var pack = await verticalPackService.GetPackAsync(code, ct);
        return pack is null ? NotFound() : Ok(pack);
    }

    [HttpGet("current")]
    [RequirePermission("Vertical.View")]
    public async Task<IActionResult> GetShopVertical(CancellationToken ct = default)
        => Ok(await verticalPackService.GetShopVerticalAsync(ct));

    [HttpPost("install")]
    [RequirePermission("Vertical.Manage")]
    public async Task<IActionResult> Install([FromBody] InstallVerticalRequest request, CancellationToken ct = default)
        => Ok(await verticalPackService.InstallForShopAsync(request.PackCode, ct));
}

public record InstallVerticalRequest(string PackCode);
