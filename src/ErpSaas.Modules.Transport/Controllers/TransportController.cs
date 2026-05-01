using ErpSaas.Modules.Transport.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Transport.Controllers;

[Route("api/transport")]
[Authorize]
public sealed class TransportController(ITransportService transportService) : BaseController
{
    [HttpGet("providers")]
    [RequirePermission("Transport.View")]
    public async Task<IActionResult> ListProviders(CancellationToken ct = default)
        => Ok(await transportService.ListProvidersAsync(ct));

    [HttpPost("providers")]
    [RequirePermission("Transport.Manage")]
    public async Task<IActionResult> CreateProvider([FromBody] CreateTransportProviderDto dto, CancellationToken ct = default)
        => Ok(await transportService.CreateProviderAsync(dto, ct));

    [HttpPatch("providers/{id:long}/toggle")]
    [RequirePermission("Transport.Manage")]
    public async Task<IActionResult> ToggleProvider(long id, [FromQuery] bool isActive, CancellationToken ct = default)
        => Ok(await transportService.ToggleProviderAsync(id, isActive, ct));

    [HttpGet("vehicles")]
    [RequirePermission("Transport.View")]
    public async Task<IActionResult> ListVehicles(CancellationToken ct = default)
        => Ok(await transportService.ListVehiclesAsync(ct));

    [HttpPost("vehicles")]
    [RequirePermission("Transport.Manage")]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleDto dto, CancellationToken ct = default)
        => Ok(await transportService.CreateVehicleAsync(dto, ct));

    [HttpPatch("vehicles/{id:long}/toggle")]
    [RequirePermission("Transport.Manage")]
    public async Task<IActionResult> ToggleVehicle(long id, [FromQuery] bool isActive, CancellationToken ct = default)
        => Ok(await transportService.ToggleVehicleAsync(id, isActive, ct));

    [HttpGet("deliveries")]
    [RequirePermission("Transport.View")]
    public async Task<IActionResult> ListDeliveries(CancellationToken ct = default)
        => Ok(await transportService.ListDeliveriesAsync(ct));

    [HttpPost("deliveries")]
    [RequirePermission("Transport.Manage")]
    public async Task<IActionResult> CreateDelivery([FromBody] CreateDeliveryDto dto, CancellationToken ct = default)
        => Ok(await transportService.CreateDeliveryAsync(dto, ct));

    [HttpPatch("deliveries/{id:long}/status")]
    [RequirePermission("Transport.Manage")]
    public async Task<IActionResult> UpdateDeliveryStatus(long id, [FromBody] UpdateDeliveryStatusDto dto, CancellationToken ct = default)
        => Ok(await transportService.UpdateDeliveryStatusAsync(id, dto, ct));
}
