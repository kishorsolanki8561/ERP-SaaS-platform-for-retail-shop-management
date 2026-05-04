using ErpSaas.Infrastructure.Data.Entities.Replication;
using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Identity.Controllers;

[Route("api/on-prem")]
[Authorize]
public sealed class OnPremDeploymentController(IOnPremDeploymentService service) : BaseController
{
    [HttpPost("deployments/register")]
    [RequirePermission("OnPrem.Manage")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDeploymentDto dto, CancellationToken ct)
    {
        var result = await service.RegisterAsync(dto, ct);
        return result.IsSuccess ? Ok(result.Value) : Ok(result);
    }

    [HttpGet("deployments")]
    [RequirePermission("OnPrem.View")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await service.ListAsync(ct);
        return Ok(items);
    }

    [HttpGet("deployments/{id:long}")]
    [RequirePermission("OnPrem.View")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var result = await service.GetAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : Ok(result);
    }

    [HttpPatch("deployments/{id:long}/status")]
    [RequirePermission("OnPrem.Manage")]
    public async Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] UpdateStatusRequest req,
        CancellationToken ct)
    {
        var result = await service.UpdateStatusAsync(id, req.Status, ct);
        return result.IsSuccess ? NoContent() : Ok(result);
    }

    [HttpPatch("deployments/{id:long}/mode")]
    [RequirePermission("OnPrem.Manage")]
    public async Task<IActionResult> UpdateMode(
        long id,
        [FromBody] UpdateModeRequest req,
        CancellationToken ct)
    {
        var result = await service.UpdateModeAsync(id, req.Mode, ct);
        return result.IsSuccess ? NoContent() : Ok(result);
    }

    [HttpGet("deployments/{deploymentId:long}/logs")]
    [RequirePermission("OnPrem.View")]
    public async Task<IActionResult> ListLogs(
        long deploymentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var items = await service.ListLogsAsync(deploymentId, page, pageSize, ct);
        return Ok(new { items, page, pageSize });
    }

    [HttpGet("conflicts")]
    [RequirePermission("OnPrem.View")]
    public async Task<IActionResult> ListConflicts(
        [FromQuery] long? deploymentId,
        [FromQuery] ConflictResolutionOutcome? outcome,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, total) = await service.ListConflictsAsync(deploymentId, outcome, page, pageSize, ct);
        return Ok(new { items, totalCount = total, page, pageSize });
    }

    [HttpPost("conflicts/{conflictId:long}/resolve")]
    [RequirePermission("OnPrem.Manage")]
    public async Task<IActionResult> ResolveConflict(
        long conflictId,
        [FromBody] ResolveConflictDto dto,
        CancellationToken ct)
    {
        var result = await service.ResolveConflictAsync(conflictId, dto, ct);
        return result.IsSuccess ? NoContent() : Ok(result);
    }
}

public sealed record UpdateStatusRequest(OnPremDeploymentStatus Status);
public sealed record UpdateModeRequest(ReplicationMode Mode);
