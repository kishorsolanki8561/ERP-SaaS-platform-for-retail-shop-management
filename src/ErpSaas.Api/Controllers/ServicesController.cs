using ErpSaas.Shared.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Api.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController(IServiceCatalog catalog) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() => Ok(catalog.GetAll());
}
