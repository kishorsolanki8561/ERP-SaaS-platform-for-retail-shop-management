using ErpSaas.Modules.CustomerPortal.Entities;
using ErpSaas.Modules.CustomerPortal.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using ErpSaas.Shared.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.CustomerPortal.Controllers;

/// <summary>
/// Staff-side management of online orders from the customer portal.
/// </summary>
[Route("api/online-orders")]
[Authorize]
[ApiController]
public sealed class OnlineOrdersController(IOnlineOrderService orderService) : BaseController
{
    [HttpGet]
    [RequirePermission("OnlineOrder.View")]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Constants.Pagination.DefaultPageSize,
        [FromQuery] OnlineOrderStatus? status = null,
        CancellationToken ct = default)
        => Ok(await orderService.ListOrdersAsync(page, pageSize, status, ct));

    [HttpGet("{id:long}")]
    [RequirePermission("OnlineOrder.View")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var result = await orderService.GetOrderAsync(id, ct);
        return result.IsSuccess && result.Value is null ? NotFound() : Ok(result);
    }

    [HttpPatch("{id:long}/accept")]
    [RequirePermission("OnlineOrder.Manage")]
    public async Task<IActionResult> Accept(long id, CancellationToken ct)
        => Ok(await orderService.AcceptOrderAsync(id, ct));

    [HttpPatch("{id:long}/reject")]
    [RequirePermission("OnlineOrder.Manage")]
    public async Task<IActionResult> Reject(long id, [FromBody] RejectOrderRequest request, CancellationToken ct)
        => Ok(await orderService.RejectOrderAsync(id, request.Reason, ct));

    [HttpPatch("{id:long}/dispatch")]
    [RequirePermission("OnlineOrder.Manage")]
    public async Task<IActionResult> Dispatch(long id, CancellationToken ct)
        => Ok(await orderService.MarkDispatchedAsync(id, ct));

    [HttpPatch("{id:long}/deliver")]
    [RequirePermission("OnlineOrder.Manage")]
    public async Task<IActionResult> Deliver(long id, CancellationToken ct)
        => Ok(await orderService.MarkDeliveredAsync(id, ct));

    [HttpPatch("{id:long}/cancel")]
    [RequirePermission("OnlineOrder.Manage")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
        => Ok(await orderService.CancelOrderAsync(id, ct));
}

public record RejectOrderRequest(string Reason);
