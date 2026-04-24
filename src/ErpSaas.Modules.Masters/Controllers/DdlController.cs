using ErpSaas.Infrastructure.Ddl;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Masters.Controllers;

[Route("api/ddl")]
[Authorize]
public class DdlController(IDdlService ddlService) : BaseController
{
    [HttpGet("{key}")]
    public async Task<IActionResult> GetItems(
        string key,
        [FromQuery] string? parentCode,
        CancellationToken ct)
    {
        var items = await ddlService.GetItemsAsync(key, CurrentShopId, parentCode, ct);
        return Ok(items);
    }

    [HttpGet("batch")]
    public async Task<IActionResult> GetBatch(
        [FromQuery] string[] keys,
        CancellationToken ct)
    {
        if (keys.Length == 0)
            return BadRequest("At least one key is required.");

        var result = await ddlService.GetBatchAsync(keys, CurrentShopId, ct);
        return Ok(result);
    }
}
