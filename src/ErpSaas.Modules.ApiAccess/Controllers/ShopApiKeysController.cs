using ErpSaas.Modules.ApiAccess.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.ApiAccess.Controllers;

[Route("api/shop-api-keys")]
[Authorize]
public sealed class ShopApiKeysController(IShopApiKeyService apiKeyService) : BaseController
{
    [HttpPost]
    [RequirePermission("Integration.ManageApiKeys")]
    [RequireFeature("integration.api_access")]
    public async Task<IActionResult> Create([FromBody] CreateApiKeyDto dto, CancellationToken ct = default)
        => Ok(await apiKeyService.CreateAsync(dto, CurrentUserId, ct));

    [HttpGet]
    [RequirePermission("Integration.ManageApiKeys")]
    [RequireFeature("integration.api_access")]
    public async Task<IActionResult> List(CancellationToken ct = default)
        => base.Ok(await apiKeyService.ListAsync(ct));

    [HttpPost("{id:long}/rotate")]
    [RequirePermission("Integration.ManageApiKeys")]
    [RequireFeature("integration.api_access")]
    public async Task<IActionResult> Rotate(long id, CancellationToken ct = default)
        => Ok(await apiKeyService.RotateAsync(id, CurrentUserId, ct));

    [HttpPost("{id:long}/revoke")]
    [RequirePermission("Integration.ManageApiKeys")]
    [RequireFeature("integration.api_access")]
    public async Task<IActionResult> Revoke(long id, [FromBody] RevokeApiKeyDto dto, CancellationToken ct = default)
        => Ok(await apiKeyService.RevokeAsync(id, dto, CurrentUserId, ct));
}
