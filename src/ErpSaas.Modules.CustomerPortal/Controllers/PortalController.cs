using ErpSaas.Modules.CustomerPortal.Entities;
using ErpSaas.Modules.CustomerPortal.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using ErpSaas.Shared.Messages;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ErpSaas.Modules.CustomerPortal.Controllers;

[Route("api/portal")]
[ApiController]
[CustomerAuth]
public sealed class PortalController(
    ICustomerPortalService portalService,
    IOnlineOrderService orderService,
    ICustomerInquiryService inquiryService) : BaseController
{
    private long CurrentPortalCustomerId =>
        long.Parse(User.FindFirstValue("customer_id") ?? "0");

    // ── Profile ───────────────────────────────────────────────────────────────

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
        => Ok(await portalService.GetProfileAsync(CurrentPortalCustomerId, ct));

    [HttpPatch("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateCustomerProfileDto dto, CancellationToken ct)
        => Ok(await portalService.UpdateProfileAsync(CurrentPortalCustomerId, dto, ct));

    // ── Purchase history ──────────────────────────────────────────────────────

    [HttpGet("me/purchases")]
    public async Task<IActionResult> ListPurchases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Constants.Pagination.DefaultPageSize,
        CancellationToken ct = default)
        => Ok(await portalService.ListPurchasesAsync(CurrentPortalCustomerId, page, pageSize, ct));

    [HttpGet("me/purchases/{invoiceId:long}")]
    public async Task<IActionResult> GetPurchase(long invoiceId, CancellationToken ct)
    {
        var result = await portalService.GetPurchaseAsync(CurrentPortalCustomerId, invoiceId, ct);
        return result.IsSuccess && result.Value is null ? NotFound() : Ok(result);
    }

    // ── Insights ──────────────────────────────────────────────────────────────

    [HttpGet("me/insights")]
    public async Task<IActionResult> GetInsights(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var f = from ?? DateTime.UtcNow.AddMonths(-12);
        var t = to ?? DateTime.UtcNow;
        return Ok(await portalService.GetInsightsAsync(CurrentPortalCustomerId, f, t, ct));
    }

    // ── Shops ─────────────────────────────────────────────────────────────────

    [HttpGet("shops")]
    public async Task<IActionResult> ListShops(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Constants.Pagination.DefaultPageSize,
        CancellationToken ct = default)
        => Ok(await portalService.ListLinkedShopsAsync(CurrentPortalCustomerId, page, pageSize, ct));

    // ── Online orders (customer-side) ──────────────────────────────────────

    [HttpGet("me/orders")]
    public async Task<IActionResult> ListMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Constants.Pagination.DefaultPageSize,
        CancellationToken ct = default)
        => Ok(await orderService.ListOrdersAsync(page, pageSize, null, ct));

    [HttpPost("shops/{shopId:long}/orders")]
    public async Task<IActionResult> PlaceOrder(long shopId, [FromBody] CreateOnlineOrderDto dto, CancellationToken ct)
        => Ok(await orderService.CreateOrderAsync(dto, CurrentPortalCustomerId, ct));

    // ── Inquiries (customer-side) ──────────────────────────────────────────

    [HttpGet("me/inquiries")]
    public async Task<IActionResult> ListMyInquiries(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Constants.Pagination.DefaultPageSize,
        CancellationToken ct = default)
        => Ok(await inquiryService.ListInquiriesAsync(page, pageSize, null, ct));

    [HttpPost("shops/{shopId:long}/inquiries")]
    public async Task<IActionResult> CreateInquiry(long shopId, [FromBody] CreateInquiryDto dto, CancellationToken ct)
        => Ok(await inquiryService.CreateInquiryAsync(dto, CurrentPortalCustomerId, ct));
}
