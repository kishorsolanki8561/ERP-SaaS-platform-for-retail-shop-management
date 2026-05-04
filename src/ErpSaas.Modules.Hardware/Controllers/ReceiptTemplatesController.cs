using ErpSaas.Modules.Hardware.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Hardware.Controllers;

[Route("api/receipt-templates")]
[Authorize]
public sealed class ReceiptTemplatesController(IReceiptTemplateService service) : BaseController
{
    [HttpGet]
    [RequirePermission("Template.Receipt.Manage")]
    public async Task<IActionResult> List(CancellationToken ct = default)
        => Ok(await service.ListAsync(ct));

    [HttpGet("{id:long}")]
    [RequirePermission("Template.Receipt.Manage")]
    public async Task<IActionResult> Get(long id, CancellationToken ct = default)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("Template.Receipt.Manage")]
    public async Task<IActionResult> Create(
        [FromBody] CreateReceiptTemplateDto dto,
        CancellationToken ct = default)
        => Ok(await service.CreateAsync(dto, ct));

    [HttpPatch("{id:long}")]
    [RequirePermission("Template.Receipt.Manage")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateReceiptTemplateDto dto,
        CancellationToken ct = default)
        => Ok(await service.UpdateAsync(id, dto, ct));

    [HttpDelete("{id:long}")]
    [RequirePermission("Template.Receipt.Manage")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct = default)
        => Ok(await service.DeleteAsync(id, ct));

    [HttpPost("{id:long}/preview")]
    [RequirePermission("Template.Receipt.Manage")]
    public async Task<IActionResult> Preview(
        long id,
        [FromBody] PrintReceiptRequest request,
        CancellationToken ct = default)
        => Ok(await service.RenderAsync(request with { ReceiptTemplateId = id }, ct));
}
