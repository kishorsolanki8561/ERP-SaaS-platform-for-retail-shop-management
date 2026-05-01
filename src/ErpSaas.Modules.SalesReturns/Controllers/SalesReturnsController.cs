using ErpSaas.Modules.SalesReturns.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.SalesReturns.Controllers;

[Route("api/sales-returns")]
[Authorize]
public sealed class SalesReturnsController(ISalesReturnsService salesReturnsService) : BaseController
{
    [HttpPost]
    [RequirePermission("SalesReturns.Create")]
    public async Task<IActionResult> CreateSalesReturn([FromBody] CreateSalesReturnDto dto, CancellationToken ct = default)
        => Ok(await salesReturnsService.CreateSalesReturnAsync(dto, ct));

    [HttpPost("{id:long}/approve")]
    [RequirePermission("SalesReturns.Approve")]
    public async Task<IActionResult> ApproveSalesReturn(long id, CancellationToken ct = default)
        => Ok(await salesReturnsService.ApproveSalesReturnAsync(id, ct));

    [HttpPost("{id:long}/cancel")]
    [RequirePermission("SalesReturns.Create")]
    public async Task<IActionResult> CancelSalesReturn(long id, CancellationToken ct = default)
        => Ok(await salesReturnsService.CancelSalesReturnAsync(id, ct));

    [HttpPost("credit-notes")]
    [RequirePermission("SalesReturns.Approve")]
    public async Task<IActionResult> IssueCreditNote([FromBody] IssueCreditNoteDto dto, CancellationToken ct = default)
        => Ok(await salesReturnsService.IssueCreditNoteAsync(dto, ct));

    [HttpPost("credit-notes/apply")]
    [RequirePermission("SalesReturns.Approve")]
    public async Task<IActionResult> ApplyCreditNote([FromBody] ApplyCreditNoteDto dto, CancellationToken ct = default)
        => Ok(await salesReturnsService.ApplyCreditNoteAsync(dto, ct));

    [HttpPost("credit-notes/{id:long}/cancel")]
    [RequirePermission("SalesReturns.Approve")]
    public async Task<IActionResult> CancelCreditNote(long id, CancellationToken ct = default)
        => Ok(await salesReturnsService.CancelCreditNoteAsync(id, ct));
}
