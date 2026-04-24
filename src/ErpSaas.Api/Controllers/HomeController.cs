using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Api.Controllers;

[Route("")]
public class HomeController(IServiceCatalog catalog) : BaseController
{
    [HttpGet]
    [Produces("text/html")]
    public ContentResult Index()
    {
        var services = catalog.GetAll();
        var rows = string.Join("\n", services.Select(s =>
            $"<tr><td>{s.Name}</td><td>{s.Description}</td><td>{s.Version}</td></tr>"));

        var html = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8"><title>ShopEarth ERP — Services</title>
            <style>body{font-family:system-ui;padding:2rem;}table{border-collapse:collapse;width:100%;}
            th,td{border:1px solid #ccc;padding:.5rem 1rem;text-align:left;}th{background:#f5f5f5;}</style>
            </head>
            <body>
            <h1>ShopEarth ERP Platform</h1>
            <p>Phase 0 — Foundation</p>
            <table>
              <thead><tr><th>Service</th><th>Description</th><th>Version</th></tr></thead>
              <tbody>{{rows}}</tbody>
            </table>
            <p><a href="/swagger">Swagger UI</a> | <a href="/api/services">JSON</a></p>
            </body></html>
            """;

        return Content(html, "text/html");
    }
}
