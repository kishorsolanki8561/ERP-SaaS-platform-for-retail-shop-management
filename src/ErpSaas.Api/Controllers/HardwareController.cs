using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Api.Controllers;

[Route("api/hardware")]
[Authorize]
public sealed class HardwareController : BaseController
{
    [HttpPost("cash-drawer/pop")]
    [RequirePermission("Hardware.CashDrawer")]
    public IActionResult PopCashDrawer() => NoContent();
}
