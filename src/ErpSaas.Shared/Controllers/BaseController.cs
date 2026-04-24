using ErpSaas.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Shared.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected long CurrentUserId =>
        long.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : 0;

    protected long CurrentShopId =>
        long.TryParse(User.FindFirst("shop_id")?.Value, out var id) ? id : 0;

    protected IActionResult Ok<T>(Result<T> result) => result.ToActionResult();
    protected IActionResult Ok(Result result) => result.ToActionResult();
}
