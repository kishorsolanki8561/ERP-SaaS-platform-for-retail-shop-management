using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace ErpSaas.Api.Controllers;

[Route("api")]
[AllowAnonymous]
public sealed class SystemController(IServiceCatalog catalog) : BaseController
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });

    [HttpGet("version")]
    public IActionResult Version()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "1.0.0";
        return Ok(new { version, phase = "Phase 0" });
    }

    [HttpGet("services")]
    public IActionResult Services() => Ok(catalog.GetAll());
}
