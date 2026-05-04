using ErpSaas.Modules.ApiAccess.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.ApiAccess.Controllers;

[Route("api/webhooks")]
[Authorize]
public sealed class WebhooksController(IWebhookDispatchService webhooks) : BaseController
{
    [HttpGet("events")]
    [RequirePermission("Integration.ManageWebhooks")]
    public IActionResult GetEventCatalog()
        => base.Ok(webhooks.GetEventCatalog());

    [HttpPost("endpoints")]
    [RequirePermission("Integration.ManageWebhooks")]
    [RequireFeature("integration.webhooks")]
    public async Task<IActionResult> RegisterEndpoint([FromBody] RegisterEndpointDto dto, CancellationToken ct = default)
        => Ok(await webhooks.RegisterEndpointAsync(dto, ct));

    [HttpGet("endpoints")]
    [RequirePermission("Integration.ManageWebhooks")]
    [RequireFeature("integration.webhooks")]
    public async Task<IActionResult> ListEndpoints(CancellationToken ct = default)
        => base.Ok(await webhooks.ListEndpointsAsync(ct));

    [HttpPatch("endpoints/{id:long}")]
    [RequirePermission("Integration.ManageWebhooks")]
    [RequireFeature("integration.webhooks")]
    public async Task<IActionResult> UpdateEndpoint(long id, [FromBody] UpdateEndpointDto dto, CancellationToken ct = default)
        => Ok(await webhooks.UpdateEndpointAsync(id, dto, ct));

    [HttpPost("endpoints/{id:long}/rotate-secret")]
    [RequirePermission("Integration.ManageWebhooks")]
    [RequireFeature("integration.webhooks")]
    public async Task<IActionResult> RotateSecret(long id, CancellationToken ct = default)
        => Ok(await webhooks.RotateSecretAsync(id, ct));

    [HttpPost("endpoints/{id:long}/test")]
    [RequirePermission("Integration.ManageWebhooks")]
    [RequireFeature("integration.webhooks")]
    public async Task<IActionResult> TestEndpoint(long id, CancellationToken ct = default)
    {
        var testPayload = new { event_type = "test.ping", timestamp = DateTime.UtcNow, message = "Test delivery from ShopSphere" };
        await webhooks.DispatchAsync(CurrentShopId, "test.ping", testPayload, ct);
        return base.Ok(new { message = "Test event dispatched" });
    }

    [HttpGet("deliveries")]
    [RequirePermission("Integration.ViewDeliveries")]
    [RequireFeature("integration.webhooks")]
    public async Task<IActionResult> ListDeliveries([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => base.Ok(await webhooks.ListDeliveriesAsync(page, pageSize, ct));

    [HttpPost("deliveries/{id:long}/retry")]
    [RequirePermission("Integration.ManageWebhooks")]
    [RequireFeature("integration.webhooks")]
    public async Task<IActionResult> RetryDelivery(long id, CancellationToken ct = default)
        => Ok(await webhooks.RetryDeliveryAsync(id, ct));
}
