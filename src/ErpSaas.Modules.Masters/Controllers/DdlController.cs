using ErpSaas.Infrastructure.Ddl;
using ErpSaas.Shared.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Masters.Controllers;

[ApiController]
[Route("api/ddl")]
[Authorize]
public class DdlController(IDdlService ddlService, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("{key}")]
    public async Task<IActionResult> GetItems(
        string key,
        [FromQuery] string? parentCode,
        CancellationToken ct)
    {
        var items = await ddlService.GetItemsAsync(key, tenantContext.ShopId, parentCode, ct);
        return Ok(items);
    }

    [HttpGet("batch")]
    public async Task<IActionResult> GetBatch(
        [FromQuery] string[] keys,
        CancellationToken ct)
    {
        if (keys.Length == 0)
            return BadRequest("At least one key is required.");

        var result = await ddlService.GetBatchAsync(keys, tenantContext.ShopId, ct);
        return Ok(result);
    }
}
