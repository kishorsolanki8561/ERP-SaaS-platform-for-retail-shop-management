using ErpSaas.Modules.ServiceJobs.Enums;
using ErpSaas.Modules.ServiceJobs.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.ServiceJobs.Controllers;

[Route("api/service-jobs")]
[Authorize]
public sealed class ServiceJobsController(IServiceJobService serviceJobService) : BaseController
{
    [HttpGet]
    [RequirePermission("ServiceJob.View")]
    public async Task<IActionResult> List([FromQuery] ServiceJobStatus? status, CancellationToken ct = default)
        => Ok(await serviceJobService.ListAsync(status, ct));

    [HttpGet("{id:long}")]
    [RequirePermission("ServiceJob.View")]
    public async Task<IActionResult> Get(long id, CancellationToken ct = default)
    {
        var result = await serviceJobService.GetAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("track/{jobNumber}")]
    [AllowAnonymous]
    public async Task<IActionResult> Track(string jobNumber, CancellationToken ct = default)
    {
        var result = await serviceJobService.GetByJobNumberAsync(jobNumber, ct);
        if (result is null) return NotFound();
        // Return only public-safe fields for the tracking page
        return Ok(new { result.JobNumber, result.Status, result.ItemDescription, result.EstimatedCost, result.TotalCost });
    }

    [HttpPost]
    [RequirePermission("ServiceJob.Create")]
    public async Task<IActionResult> Receive([FromBody] ReceiveServiceJobDto dto, CancellationToken ct = default)
        => Ok(await serviceJobService.ReceiveAsync(dto, ct));

    [HttpPost("{id:long}/diagnose")]
    [RequirePermission("ServiceJob.Diagnose")]
    public async Task<IActionResult> Diagnose(long id, [FromBody] DiagnoseServiceJobDto dto, CancellationToken ct = default)
        => Ok(await serviceJobService.DiagnoseAsync(id, dto, ct));

    [HttpPost("{id:long}/customer-approve")]
    [RequirePermission("ServiceJob.Approve")]
    public async Task<IActionResult> CustomerApprove(long id, CancellationToken ct = default)
        => Ok(await serviceJobService.CustomerApproveAsync(id, ct));

    [HttpPost("{id:long}/progress")]
    [RequirePermission("ServiceJob.Progress")]
    public async Task<IActionResult> StartProgress(long id, CancellationToken ct = default)
        => Ok(await serviceJobService.StartProgressAsync(id, ct));

    [HttpPost("{id:long}/ready")]
    [RequirePermission("ServiceJob.Progress")]
    public async Task<IActionResult> MarkReady(long id, CancellationToken ct = default)
        => Ok(await serviceJobService.MarkReadyAsync(id, ct));

    [HttpPost("{id:long}/deliver")]
    [RequirePermission("ServiceJob.Deliver")]
    public async Task<IActionResult> Deliver(long id, CancellationToken ct = default)
        => Ok(await serviceJobService.DeliverAsync(id, ct));

    [HttpPost("{id:long}/reject")]
    [RequirePermission("ServiceJob.Approve")]
    public async Task<IActionResult> Reject(long id, [FromBody] RejectRequest req, CancellationToken ct = default)
        => Ok(await serviceJobService.RejectAsync(id, req.Reason, ct));

    [HttpPost("{id:long}/parts")]
    [RequirePermission("ServiceJob.Progress")]
    public async Task<IActionResult> AddPart(long id, [FromBody] AddPartDto dto, CancellationToken ct = default)
        => Ok(await serviceJobService.AddPartAsync(id, dto, ct));

    [HttpPost("{id:long}/labor")]
    [RequirePermission("ServiceJob.Progress")]
    public async Task<IActionResult> AddLabor(long id, [FromBody] AddLaborDto dto, CancellationToken ct = default)
        => Ok(await serviceJobService.AddLaborAsync(id, dto, ct));
}

public record RejectRequest(string Reason);
