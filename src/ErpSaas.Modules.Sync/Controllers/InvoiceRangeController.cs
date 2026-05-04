using ErpSaas.Modules.Sync.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Sync.Controllers;

[Route("api/invoice-ranges")]
[Authorize]
public sealed class InvoiceRangeController(ISyncService syncService) : BaseController
{
    [HttpPost]
    [RequirePermission("Device.Register")]
    public async Task<IActionResult> Allocate([FromBody] AllocateInvoiceRangeDto dto, CancellationToken ct)
    {
        var result = await syncService.AllocateInvoiceRangeAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("{allocationId:long}/release")]
    [RequirePermission("Device.Register")]
    public async Task<IActionResult> Release(long allocationId, [FromBody] ReleaseInvoiceRangeDto dto, CancellationToken ct)
    {
        var result = await syncService.ReleaseInvoiceRangeAsync(allocationId, dto, ct);
        return Ok(result);
    }
}
