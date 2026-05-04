using ErpSaas.Modules.Hardware.Enums;
using ErpSaas.Modules.Hardware.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Hardware.Controllers;

[Route("api/label-templates")]
[Authorize]
public sealed class LabelTemplatesController(ILabelTemplateService service) : BaseController
{
    [HttpGet]
    [RequirePermission("Template.Label.Manage")]
    public async Task<IActionResult> List(
        [FromQuery] LabelType? labelType = null,
        CancellationToken ct = default)
        => Ok(await service.ListAsync(labelType, ct));

    [HttpGet("{id:long}")]
    [RequirePermission("Template.Label.Manage")]
    public async Task<IActionResult> Get(long id, CancellationToken ct = default)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("Template.Label.Manage")]
    public async Task<IActionResult> Create(
        [FromBody] CreateLabelTemplateDto dto,
        CancellationToken ct = default)
        => Ok(await service.CreateAsync(dto, ct));

    [HttpPatch("{id:long}")]
    [RequirePermission("Template.Label.Manage")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateLabelTemplateDto dto,
        CancellationToken ct = default)
        => Ok(await service.UpdateAsync(id, dto, ct));

    [HttpDelete("{id:long}")]
    [RequirePermission("Template.Label.Manage")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct = default)
        => Ok(await service.DeleteAsync(id, ct));
}
