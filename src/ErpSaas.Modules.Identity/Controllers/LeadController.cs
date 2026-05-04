using ErpSaas.Infrastructure.Data.Entities.Marketing;
using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Identity.Controllers;

[Route("api/leads")]
public sealed class LeadController(ILeadService leadService) : BaseController
{
    [HttpPost]
    [AllowAnonymous]
    [RequireCaptcha]
    public async Task<IActionResult> Submit([FromBody] SubmitLeadDto dto, CancellationToken ct)
    {
        var result = await leadService.SubmitAsync(dto, ct);
        return Ok(result);
    }

    [HttpGet]
    [Authorize]
    [RequirePermission("Lead.View")]
    public async Task<IActionResult> List(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        LeadStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LeadStatus>(status, ignoreCase: true, out var parsed))
            statusFilter = parsed;

        var (items, total) = await leadService.ListAsync(page, pageSize, statusFilter, ct);
        return Ok(new { items, totalCount = total, page, pageSize });
    }

    [HttpGet("{id:long}")]
    [Authorize]
    [RequirePermission("Lead.View")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var detail = await leadService.GetAsync(id, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPatch("{id:long}")]
    [Authorize]
    [RequirePermission("Lead.Edit")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateLeadStatusDto dto, CancellationToken ct)
    {
        var result = await leadService.UpdateStatusAsync(id, dto, ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/assign")]
    [Authorize]
    [RequirePermission("Lead.Assign")]
    public async Task<IActionResult> Assign(long id, [FromBody] AssignLeadDto dto, CancellationToken ct)
    {
        var result = await leadService.AssignAsync(id, dto.UserId, ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/convert")]
    [Authorize]
    [RequirePermission("Lead.Convert")]
    public async Task<IActionResult> Convert(long id, CancellationToken ct)
    {
        var result = await leadService.ConvertAsync(id, ct);
        return Ok(result);
    }
}

public sealed record AssignLeadDto(long UserId);
