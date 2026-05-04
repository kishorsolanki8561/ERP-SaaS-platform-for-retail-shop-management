using ErpSaas.Infrastructure.Metering;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Identity.Controllers;

[Route("api/usage")]
[Authorize]
public sealed class UsageController(IUsageMeterService usageMeter) : BaseController
{
    [HttpGet("current")]
    [RequirePermission("Usage.View")]
    public async Task<IActionResult> GetCurrentUsage(CancellationToken ct)
        => Ok(await usageMeter.GetCurrentUsageAsync(ct));

    [HttpGet("history")]
    [RequirePermission("Usage.ViewHistory")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] string? meterCode,
        [FromQuery] int months = 6,
        CancellationToken ct = default)
        => Ok(await usageMeter.GetHistoryAsync(meterCode, months, ct));

    [HttpGet("forecast")]
    [RequirePermission("Usage.View")]
    public async Task<IActionResult> GetForecast(CancellationToken ct)
        => Ok(await usageMeter.GetForecastAsync(ct));

    [HttpGet("events")]
    [RequirePermission("Usage.ViewHistory")]
    public async Task<IActionResult> GetEvents(
        [FromQuery] string? meterCode,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await usageMeter.GetEventsAsync(meterCode, from, to, page, pageSize, ct);
        return Ok(new { items, totalCount });
    }
}
