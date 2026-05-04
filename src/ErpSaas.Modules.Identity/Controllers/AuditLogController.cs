using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using ErpSaas.Shared.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Identity.Controllers;

[Route("api/admin/audit-logs")]
[Authorize]
public sealed class AuditLogController(
    IAuditLogService auditLogService,
    ITenantContext tenantContext) : BaseController
{
    [HttpGet]
    [RequirePermission("Admin.AuditLog.View")]
    public async Task<IActionResult> List(
        [FromQuery] string entityType,
        [FromQuery] string? entityId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // Shop-level admins are implicitly scoped to their shop.
        // Platform owners (ShopId=0) can see all shops — pass null to skip shop filter.
        long? shopId = tenantContext.ShopId == 0 ? null : tenantContext.ShopId;

        var result = await auditLogService.ListAsync(
            entityType, entityId, from, to, shopId, pageNumber, pageSize, ct);

        return Ok(result);
    }
}
