using ErpSaas.Modules.Quotations.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Quotations.Controllers;

[Route("api/quotations")]
[Authorize]
public sealed class QuotationsController(IQuotationsService quotationsService) : BaseController
{
    [HttpGet]
    [RequirePermission("Quotation.View")]
    public async Task<IActionResult> ListQuotations(CancellationToken ct = default)
        => Ok(await quotationsService.ListQuotationsAsync(ct));

    [HttpPost]
    [RequirePermission("Quotation.Manage")]
    public async Task<IActionResult> CreateQuotation([FromBody] CreateQuotationDto dto, CancellationToken ct = default)
        => Ok(await quotationsService.CreateQuotationAsync(dto, ct));

    [HttpPatch("{id:long}/send")]
    [RequirePermission("Quotation.Manage")]
    public async Task<IActionResult> SendQuotation(long id, CancellationToken ct = default)
        => Ok(await quotationsService.SendQuotationAsync(id, ct));

    [HttpPost("{id:long}/convert")]
    [RequirePermission("Quotation.Manage")]
    public async Task<IActionResult> ConvertToSalesOrder(long id, CancellationToken ct = default)
        => Ok(await quotationsService.ConvertQuotationToSalesOrderAsync(id, ct));

    [HttpPatch("{id:long}/reject")]
    [RequirePermission("Quotation.Manage")]
    public async Task<IActionResult> RejectQuotation(long id, CancellationToken ct = default)
        => Ok(await quotationsService.RejectQuotationAsync(id, ct));

    [HttpGet("sales-orders")]
    [RequirePermission("Quotation.View")]
    public async Task<IActionResult> ListSalesOrders(CancellationToken ct = default)
        => Ok(await quotationsService.ListSalesOrdersAsync(ct));

    [HttpPost("sales-orders")]
    [RequirePermission("Quotation.Manage")]
    public async Task<IActionResult> CreateSalesOrder([FromBody] CreateSalesOrderDto dto, CancellationToken ct = default)
        => Ok(await quotationsService.CreateSalesOrderAsync(dto, ct));

    [HttpPatch("sales-orders/{id:long}/cancel")]
    [RequirePermission("Quotation.Manage")]
    public async Task<IActionResult> CancelSalesOrder(long id, CancellationToken ct = default)
        => Ok(await quotationsService.CancelSalesOrderAsync(id, ct));

    [HttpGet("delivery-challans")]
    [RequirePermission("Quotation.View")]
    public async Task<IActionResult> ListDeliveryChallans(CancellationToken ct = default)
        => Ok(await quotationsService.ListDeliveryChallansAsync(ct));

    [HttpPost("delivery-challans")]
    [RequirePermission("Quotation.Manage")]
    public async Task<IActionResult> CreateDeliveryChallan([FromBody] CreateDeliveryChallanDto dto, CancellationToken ct = default)
        => Ok(await quotationsService.CreateDeliveryChallanAsync(dto, ct));

    [HttpPatch("delivery-challans/{id:long}/dispatch")]
    [RequirePermission("Quotation.Manage")]
    public async Task<IActionResult> DispatchDeliveryChallan(long id, CancellationToken ct = default)
        => Ok(await quotationsService.DispatchDeliveryChallanAsync(id, ct));

    [HttpPatch("delivery-challans/{id:long}/delivered")]
    [RequirePermission("Quotation.Manage")]
    public async Task<IActionResult> MarkDelivered(long id, CancellationToken ct = default)
        => Ok(await quotationsService.MarkDeliveryChallanDeliveredAsync(id, ct));
}
