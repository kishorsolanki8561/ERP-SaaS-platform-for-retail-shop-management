using ErpSaas.Modules.Hardware.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Hardware.Controllers;

[Route("api/print")]
[Authorize]
public sealed class PrintController(
    ILabelTemplateService labelService,
    IReceiptTemplateService receiptService) : BaseController
{
    /// <summary>
    /// Renders ZPL for the specified product. The client delivers the ZPL string
    /// to the local label printer (USB / network) via Electron or Capacitor bridge.
    /// </summary>
    [HttpPost("label")]
    [RequirePermission("Template.Label.Manage")]
    [RequireFeature("hardware.label_printer")]
    public async Task<IActionResult> PrintLabel(
        [FromBody] PrintLabelRequest request,
        CancellationToken ct = default)
        => Ok(await labelService.RenderAsync(request, ct));

    /// <summary>
    /// Renders ESC/POS bytes (base64) for the receipt. The client sends the bytes
    /// to the thermal printer via Electron / Capacitor bridge.
    /// </summary>
    [HttpPost("receipt")]
    [RequirePermission("Template.Receipt.Manage")]
    [RequireFeature("hardware.thermal_receipt")]
    public async Task<IActionResult> PrintReceipt(
        [FromBody] PrintReceiptRequest request,
        CancellationToken ct = default)
        => Ok(await receiptService.RenderAsync(request, ct));
}
