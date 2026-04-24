using ErpSaas.Modules.{Module}.Services;
using ErpSaas.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.{Module}.Controllers;

[ApiController]
[Route("api/{resource}")]
[Authorize]                                  // every staff endpoint requires auth
public class {Module}Controller : ControllerBase
{
    private readonly I{Module}Service _service;

    public {Module}Controller(I{Module}Service service) => _service = service;

    [HttpGet("{id:long}")]
    [RequirePermission("{Module}.View")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var result = await _service.GetAsync(id, ct);
        return result.ToActionResult();
    }

    [HttpGet]
    [RequirePermission("{Module}.View")]
    public async Task<IActionResult> List([FromQuery] {EntityName}ListFilter filter, CancellationToken ct)
    {
        var result = await _service.ListAsync(filter, ct);
        return result.ToActionResult();
    }

    [HttpPost]
    [RequirePermission("{Module}.Create")]
    [RequireFeature("{Module}.{Feature}")]   // remove if always-on
    public async Task<IActionResult> Create([FromBody] {EntityName}Dto dto, CancellationToken ct)
    {
        var result = await _service.CreateAsync(dto, ct);
        return result.ToActionResult(success: id => CreatedAtAction(nameof(Get), new { id }, new { id }));
    }

    [HttpPatch("{id:long}")]
    [RequirePermission("{Module}.Edit")]
    public async Task<IActionResult> Update(long id, [FromBody] {EntityName}Dto dto, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, dto, ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:long}")]
    [RequirePermission("{Module}.Delete")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        return result.ToActionResult();
    }
}
