using ErpSaas.Modules.Payment.Services;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Payment.Controllers;

/// <summary>
/// Single dynamic webhook endpoint — no [Authorize] because gateways call this directly.
/// Authentication is done via HMAC signature verification inside HandleWebhookAsync.
/// Register the URL pattern /api/webhooks/{gatewayCode} in each gateway's dashboard.
/// e.g. Razorpay → https://your-domain/api/webhooks/Razorpay
///      Stripe   → https://your-domain/api/webhooks/Stripe
/// </summary>
[Route("api/webhooks")]
public sealed class PaymentWebhookController(IPaymentGatewayService gatewayService) : BaseController
{
    [HttpPost("{gatewayCode}")]
    public async Task<IActionResult> Receive(string gatewayCode, CancellationToken ct = default)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var rawPayload = await reader.ReadToEndAsync(ct);
        Request.Body.Position = 0;

        var signature = Request.Headers["X-Razorpay-Signature"].FirstOrDefault()
            ?? Request.Headers["Stripe-Signature"].FirstOrDefault()
            ?? Request.Headers["X-Verify"].FirstOrDefault()
            ?? string.Empty;

        await gatewayService.HandleWebhookAsync(gatewayCode, rawPayload, signature, ct);

        // Always return 200 to prevent gateway retry storms — errors are logged internally
        return Ok(new { received = true });
    }
}
