using ErpSaas.Modules.Verticals.Medical.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Verticals.Medical.Controllers;

[Route("api/medical")]
[Authorize]
[RequireFeature("Verticals.MedicalBatchExpiry")]
public sealed class MedicalInventoryController(IMedicalInventoryService medicalService) : BaseController
{
    [HttpGet("batches")]
    [RequirePermission("Medical.Batch.View")]
    public async Task<IActionResult> ListBatches(
        [FromQuery] long? productId,
        [FromQuery] bool? expiringWithin30Days,
        CancellationToken ct = default)
        => Ok(await medicalService.ListBatchesAsync(productId, expiringWithin30Days, ct));

    [HttpGet("batches/expiring")]
    [RequirePermission("Medical.Batch.View")]
    public async Task<IActionResult> ListExpiring([FromQuery] int days = 30, CancellationToken ct = default)
        => Ok(await medicalService.ListExpiringAsync(days, ct));

    [HttpGet("batches/{id:long}")]
    [RequirePermission("Medical.Batch.View")]
    public async Task<IActionResult> GetBatch(long id, CancellationToken ct = default)
    {
        var result = await medicalService.GetBatchAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("batches/by-product/{productId:long}")]
    [RequirePermission("Medical.Batch.View")]
    public async Task<IActionResult> ListByProduct(long productId, CancellationToken ct = default)
        => Ok(await medicalService.ListBatchesByProductAsync(productId, ct));

    [HttpPost("batches")]
    [RequirePermission("Medical.Batch.Manage")]
    public async Task<IActionResult> CreateBatch([FromBody] CreateDrugBatchDto dto, CancellationToken ct = default)
        => Ok(await medicalService.CreateBatchAsync(dto, ct));

    [HttpPost("prescriptions")]
    [RequirePermission("Medical.Prescription.Record")]
    public async Task<IActionResult> RecordPrescription([FromBody] RecordPrescriptionDto dto, CancellationToken ct = default)
        => Ok(await medicalService.RecordPrescriptionAsync(dto, ct));
}
