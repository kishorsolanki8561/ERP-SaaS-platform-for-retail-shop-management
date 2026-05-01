using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Modules.Accounting.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Accounting.Controllers;

[Route("api/fixed-assets")]
[Authorize]
public sealed class FixedAssetsController(IFixedAssetService fixedAssetService) : BaseController
{
    [HttpGet]
    [RequirePermission("Accounting.FixedAssets")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> List(
        [FromQuery] FixedAssetStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await fixedAssetService.ListAsync(status, page, pageSize, ct));

    [HttpPost]
    [RequirePermission("Accounting.FixedAssets")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterFixedAssetDto dto,
        CancellationToken ct = default)
        => Ok(await fixedAssetService.RegisterAsync(dto, ct));

    [HttpPost("{id:long}/retire")]
    [RequirePermission("Accounting.FixedAssets")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> Retire(long id, CancellationToken ct = default)
        => Ok(await fixedAssetService.RetireAsync(id, ct));

    [HttpPost("{id:long}/dispose")]
    [RequirePermission("Accounting.FixedAssets")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> Dispose(
        long id,
        [FromBody] DisposeFixedAssetDto dto,
        CancellationToken ct = default)
        => Ok(await fixedAssetService.DisposeAsync(id, dto, ct));

    [HttpGet("{id:long}/depreciation-schedule")]
    [RequirePermission("Accounting.FixedAssets")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> DepreciationSchedule(long id, CancellationToken ct = default)
        => Ok(await fixedAssetService.GetDepreciationScheduleAsync(id, ct));
}

[Route("api/depreciation")]
[Authorize]
public sealed class DepreciationController(IFixedAssetService fixedAssetService) : BaseController
{
    [HttpPost("run")]
    [RequirePermission("Accounting.FixedAssets")]
    [RequireFeature("Accounting.Advanced")]
    public async Task<IActionResult> Run(
        [FromQuery] DateTime? periodDate = null,
        CancellationToken ct = default)
        => Ok(await fixedAssetService.RunDepreciationAsync(
            periodDate ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1), ct));
}
