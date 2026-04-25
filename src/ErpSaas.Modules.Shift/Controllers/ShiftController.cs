using ErpSaas.Modules.Shift.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Shift.Controllers;

[Route("api/shifts")]
[Authorize]
public sealed class ShiftController(IShiftService shiftService, ITenantContext tenant) : BaseController
{
    [HttpGet("current")]
    [RequirePermission("Shift.View")]
    public async Task<IActionResult> GetCurrentShift(
        [FromQuery] long branchId,
        CancellationToken ct)
    {
        var shift = await shiftService.GetOpenShiftForCashierAsync(tenant.CurrentUserId, branchId, ct);
        return shift is null ? NotFound() : Ok(shift);
    }

    [HttpGet]
    [RequirePermission("Shift.View")]
    public async Task<IActionResult> ListShifts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Constants.Pagination.DefaultPageSize,
        [FromQuery] long? branchId = null,
        CancellationToken ct = default)
        => Ok(await shiftService.ListShiftsAsync(page, pageSize, branchId, ct));

    [HttpGet("{id:long}")]
    [RequirePermission("Shift.View")]
    public async Task<IActionResult> GetShift(long id, CancellationToken ct)
    {
        var summary = await shiftService.GetShiftSummaryAsync(id, ct);
        return summary is null ? NotFound() : Ok(summary);
    }

    [HttpPost("open")]
    [RequirePermission("Shift.Open")]
    public async Task<IActionResult> OpenShift(
        [FromBody] OpenShiftDto dto,
        CancellationToken ct)
        => Ok(await shiftService.OpenShiftAsync(dto, ct));

    [HttpPost("{id:long}/close")]
    [RequirePermission("Shift.Close")]
    public async Task<IActionResult> CloseShift(
        long id,
        [FromBody] CloseShiftDto dto,
        CancellationToken ct)
        => Ok(await shiftService.CloseShiftAsync(id, dto, ct));

    [HttpPost("{id:long}/force-close")]
    [RequirePermission("Shift.ForceClose")]
    public async Task<IActionResult> ForceCloseShift(
        long id,
        [FromBody] ForceCloseRequest req,
        CancellationToken ct)
        => Ok(await shiftService.ForceCloseAsync(id, req.Reason, ct));

    [HttpPost("{id:long}/cash-in")]
    [RequirePermission("Shift.CashMovement")]
    public async Task<IActionResult> CashIn(
        long id,
        [FromBody] CashMovementDto dto,
        CancellationToken ct)
        => Ok(await shiftService.RecordCashInAsync(id, dto, ct));

    [HttpPost("{id:long}/cash-out")]
    [RequirePermission("Shift.CashMovement")]
    public async Task<IActionResult> CashOut(
        long id,
        [FromBody] CashMovementDto dto,
        CancellationToken ct)
        => Ok(await shiftService.RecordCashOutAsync(id, dto, ct));
}

public record ForceCloseRequest(string Reason);
