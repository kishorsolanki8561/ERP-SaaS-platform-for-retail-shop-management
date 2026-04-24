using ErpSaas.Modules.Crm.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Crm.Controllers;

[Route("api/crm")]
[Authorize]
public sealed class CrmController(ICrmService crm) : BaseController
{
    [HttpGet("customers")]
    [RequirePermission("Crm.View")]
    public async Task<IActionResult> ListCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => Ok(await crm.ListCustomersAsync(page, pageSize, search, ct));

    [HttpGet("customers/{id:long}")]
    [RequirePermission("Crm.View")]
    public async Task<IActionResult> GetCustomer(long id, CancellationToken ct = default)
    {
        var result = await crm.GetCustomerAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("customers")]
    [RequirePermission("Crm.Create")]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerDto dto, CancellationToken ct = default)
        => Ok(await crm.CreateCustomerAsync(dto, ct));

    [HttpPut("customers/{id:long}")]
    [RequirePermission("Crm.Edit")]
    public async Task<IActionResult> UpdateCustomer(
        long id, [FromBody] UpdateCustomerDto dto, CancellationToken ct = default)
        => Ok(await crm.UpdateCustomerAsync(id, dto, ct));

    [HttpDelete("customers/{id:long}")]
    [RequirePermission("Crm.Edit")]
    public async Task<IActionResult> DeactivateCustomer(long id, CancellationToken ct = default)
        => Ok(await crm.DeactivateCustomerAsync(id, ct));

    [HttpGet("groups")]
    [RequirePermission("Crm.View")]
    public async Task<IActionResult> ListGroups(CancellationToken ct = default)
        => Ok(await crm.ListGroupsAsync(ct));

    [HttpPost("groups")]
    [RequirePermission("Crm.Manage")]
    public async Task<IActionResult> CreateGroup(
        [FromBody] CreateGroupRequest req, CancellationToken ct = default)
        => Ok(await crm.CreateGroupAsync(req.Code, req.Name, req.DiscountPercent, ct));
}

public record CreateGroupRequest(string Code, string Name, decimal DiscountPercent);
