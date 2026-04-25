using ErpSaas.Infrastructure.Data;
using ErpSaas.Modules.Billing.Entities;
using ErpSaas.Modules.Billing.Enums;
using ErpSaas.Modules.Crm.Entities;
using ErpSaas.Modules.Inventory.Entities;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Api.Controllers;

public record DashboardSummaryDto(
    decimal TodaySalesAmount,
    int TodayInvoiceCount,
    string TodaySalesTrend,
    bool TodaySalesTrendUp,
    int ActiveProductCount,
    int CustomerCount);

[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(TenantDbContext db) : BaseController
{
    [HttpGet("summary")]
    [RequirePermission("Dashboard.View")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        var todaySales = await db.Set<Invoice>()
            .Where(i => !i.IsDeleted
                && i.Status != InvoiceStatus.Cancelled
                && i.InvoiceDate.Date == today)
            .SumAsync(i => (decimal?)i.GrandTotal, ct) ?? 0m;

        var yesterdaySales = await db.Set<Invoice>()
            .Where(i => !i.IsDeleted
                && i.Status != InvoiceStatus.Cancelled
                && i.InvoiceDate.Date == yesterday)
            .SumAsync(i => (decimal?)i.GrandTotal, ct) ?? 0m;

        var todayInvoiceCount = await db.Set<Invoice>()
            .CountAsync(i => !i.IsDeleted
                && i.Status != InvoiceStatus.Cancelled
                && i.InvoiceDate.Date == today, ct);

        var activeProductCount = await db.Set<Product>()
            .CountAsync(p => !p.IsDeleted && p.IsActive, ct);

        var customerCount = await db.Set<Customer>()
            .CountAsync(c => !c.IsDeleted && c.IsActive, ct);

        var (trend, trendUp) = ComputeTrend(todaySales, yesterdaySales);

        return Ok(new DashboardSummaryDto(
            todaySales, todayInvoiceCount, trend, trendUp,
            activeProductCount, customerCount));
    }

    private static (string Trend, bool Up) ComputeTrend(decimal today, decimal yesterday)
    {
        if (yesterday == 0)
            return today > 0 ? ("New", true) : ("—", true);

        var pct = (today - yesterday) / yesterday * 100m;
        var label = $"{Math.Abs(pct):F1}%";
        return pct >= 0 ? (label, true) : (label, false);
    }
}
