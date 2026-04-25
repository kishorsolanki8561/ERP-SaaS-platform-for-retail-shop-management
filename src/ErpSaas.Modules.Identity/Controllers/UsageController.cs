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
    [RequirePermission("ShopProfile.View")]
    public async Task<IActionResult> GetCurrentUsage(CancellationToken ct)
        => Ok(await usageMeter.GetCurrentUsageAsync(ct));
}
