using ErpSaas.Modules.Hardware.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Hardware.Controllers;

[Route("api/device-profiles")]
[Authorize]
public sealed class DeviceProfilesController(IDeviceProfileService service) : BaseController
{
    [HttpGet]
    [RequirePermission("Device.Configure")]
    public async Task<IActionResult> List(CancellationToken ct = default)
        => Ok(await service.ListAsync(ct));

    [HttpGet("{id:long}")]
    [RequirePermission("Device.Configure")]
    public async Task<IActionResult> Get(long id, CancellationToken ct = default)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("Device.Configure")]
    public async Task<IActionResult> Create(
        [FromBody] CreateDeviceProfileDto dto,
        CancellationToken ct = default)
        => Ok(await service.CreateAsync(dto, ct));

    [HttpPatch("{id:long}")]
    [RequirePermission("Device.Configure")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateDeviceProfileDto dto,
        CancellationToken ct = default)
        => Ok(await service.UpdateAsync(id, dto, ct));

    [HttpDelete("{id:long}")]
    [RequirePermission("Device.Configure")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct = default)
        => Ok(await service.DeleteAsync(id, ct));
}
