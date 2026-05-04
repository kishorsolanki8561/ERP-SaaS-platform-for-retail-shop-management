using ErpSaas.Modules.Sync.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Sync.Controllers;

[Route("api/sync")]
[Authorize]
public sealed class SyncController(ISyncService syncService) : BaseController
{
    [HttpPost("commands")]
    [RequirePermission("Sync.View")]
    public async Task<IActionResult> ProcessCommands([FromBody] SyncCommandsBatchDto batch, CancellationToken ct)
    {
        var result = await syncService.ProcessCommandsAsync(batch, ct);
        return Ok(result);
    }

    [HttpGet("exceptions")]
    [RequirePermission("Sync.View")]
    public async Task<IActionResult> ListExceptions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, total) = await syncService.ListExceptionsAsync(pageNumber, pageSize, ct);
        return Ok(new { items, totalCount = total, pageNumber, pageSize });
    }

    [HttpPost("exceptions/{commandId:long}/resolve")]
    [RequirePermission("Sync.ResolveException")]
    public IActionResult Resolve(long commandId)
    {
        // Resolving an exception is acknowledged by the shop admin — soft-close only.
        // Future: update OfflineCommand.Status to a resolved state.
        return Ok(new { resolved = true, commandId });
    }
}
