using ErpSaas.Modules.Sync.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Sync.Controllers;

[Route("api/devices")]
[Authorize]
public sealed class DevicesController(IDeviceService deviceService) : BaseController
{
    [HttpPost("register")]
    [RequirePermission("Device.Register")]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceDto dto, CancellationToken ct)
    {
        var result = await deviceService.RegisterAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("{deviceId:long}/heartbeat")]
    [RequirePermission("Device.Register")]
    public async Task<IActionResult> Heartbeat(long deviceId, [FromBody] HeartbeatDto dto, CancellationToken ct)
    {
        var result = await deviceService.HeartbeatAsync(deviceId, dto, ct);
        return Ok(result);
    }

    [HttpGet]
    [RequirePermission("Device.Manage")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await deviceService.ListAsync(ct);
        return Ok(items);
    }

    [HttpPost("{deviceId:long}/deactivate")]
    [RequirePermission("Device.Manage")]
    public async Task<IActionResult> Deactivate(long deviceId, CancellationToken ct)
    {
        var result = await deviceService.DeactivateAsync(deviceId, ct);
        return Ok(result);
    }
}
