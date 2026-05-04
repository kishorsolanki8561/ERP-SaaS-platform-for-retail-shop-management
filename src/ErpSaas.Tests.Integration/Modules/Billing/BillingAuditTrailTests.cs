using System.Net;
using System.Net.Http.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Modules.Billing.Entities;
using ErpSaas.Modules.Billing.Enums;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Modules.Billing;

/// <summary>
/// Verifies that every billing mutation produces the expected observable
/// state changes in the database.
///
/// NOTE: <c>BillingService</c> does not yet call <c>IAuditLogger.LogAsync</c>
/// explicitly (that is a Phase 5 polish item). <c>AuditSaveChangesInterceptor</c>
/// updates <c>CreatedAtUtc</c>/<c>UpdatedAtUtc</c> on entity rows — it does not
/// write <c>AuditLog</c> rows.  When explicit <c>IAuditLogger</c> calls are added
/// in Phase 5, assertions against <c>LogDbContext.AuditLogs</c> should be added.
///
/// Current tests assert entity-level state changes as the auditable evidence
/// that the operation occurred correctly.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class BillingAuditTrailTests(IntegrationTestFixture fixture)
{
    // ── Shared test-setup helper ──────────────────────────────────────────────

    private async Task<(long shopId, HttpClient client, long invoiceId)> ArrangeInvoiceAsync()
    {
        var (shopId, _, _) = await fixture.SeedTestUserAsync();
        var client  = fixture.CreateAuthenticatedClient(shopId: shopId, permissions: ["*"]);
        var suffix  = Guid.NewGuid().ToString("N")[..8];

        var custResp = await client.PostAsJsonAsync("/api/crm/customers", new
        {
            DisplayName  = $"Audit Customer {suffix}",
            CustomerType = "RETAIL",
            CreditLimit  = 0.0m,
        });
        custResp.StatusCode.Should().Be(HttpStatusCode.OK);
        // All Create endpoints return Result<long> → OkObjectResult(id) → plain long.
        var customerId = await custResp.Content.ReadFromJsonAsync<long>();

        var whResp = await client.PostAsJsonAsync("/api/inventory/warehouses", new
        {
            Code      = $"WH-AUD-{suffix}",
            Name      = $"Audit WH {suffix}",
            IsDefault = false,
        });
        whResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var warehouseId = await whResp.Content.ReadFromJsonAsync<long>();

        var invResp = await client.PostAsJsonAsync("/api/billing/invoices", new
        {
            InvoiceDate = DateTime.UtcNow,
            CustomerId  = customerId,
            WarehouseId = warehouseId,
            ShopId      = shopId,
            Notes       = $"Audit test invoice {suffix}",
        });
        invResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var invoiceId = await invResp.Content.ReadFromJsonAsync<long>();

        return (shopId, client, invoiceId);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateDraftInvoice_InvoiceRowPersistedWithDraftStatus()
    {
        // Arrange + Act
        var (shopId, _, invoiceId) = await ArrangeInvoiceAsync();

        // Assert — Invoice row exists with Draft status and correct shopId
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

        // IgnoreQueryFilters because the DI-scope tenant context has ShopId=0 (no HTTP request)
        var invoice = await db.Set<Invoice>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        invoice.Should().NotBeNull(
            "CreateDraftInvoice must persist an Invoice row in TenantDB");
        invoice!.Status.Should().Be(InvoiceStatus.Draft,
            "a newly created invoice must start in Draft status");
        invoice.ShopId.Should().Be(shopId,
            "the TenantSaveChangesInterceptor must stamp the shopId on the invoice row");
        invoice.CreatedAtUtc.Should().BeAfter(DateTime.UtcNow.AddMinutes(-2),
            "AuditSaveChangesInterceptor must set CreatedAtUtc on Insert");
    }

    [Fact]
    public async Task FinalizeInvoice_StatusChangesToFinalized()
    {
        // Arrange
        var (shopId, client, invoiceId) = await ArrangeInvoiceAsync();

        // Act — finalize
        var response = await client.PostAsync(
            $"/api/billing/invoices/{invoiceId}/finalize", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert — Status changed to Finalized in DB
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

        var invoice = await db.Set<Invoice>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        invoice.Should().NotBeNull();
        invoice!.Status.Should().Be(InvoiceStatus.Finalized,
            "FinalizeInvoice must transition Status from Draft → Finalized");
        invoice.UpdatedAtUtc.Should().BeAfter(invoice.CreatedAtUtc,
            "AuditSaveChangesInterceptor must update UpdatedAtUtc on the finalize mutation");
    }

    [Fact]
    public async Task CancelInvoice_StatusChangesToCancelledWithReason()
    {
        // Arrange
        var (shopId, client, invoiceId) = await ArrangeInvoiceAsync();
        const string cancelReason = "Duplicate order";

        // Act — cancel
        var response = await client.PostAsJsonAsync(
            $"/api/billing/invoices/{invoiceId}/cancel",
            new { Reason = cancelReason });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert — Status changed to Cancelled and reason appended to Notes
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

        var invoice = await db.Set<Invoice>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        invoice.Should().NotBeNull();
        invoice!.Status.Should().Be(InvoiceStatus.Cancelled,
            "CancelInvoice must transition Status to Cancelled");
        invoice.Notes.Should().Contain(cancelReason,
            "the cancellation reason must be appended to the invoice Notes field");
    }

    [Fact]
    public async Task AddLine_RecalculatesTotalsOnInvoice()
    {
        // Arrange — create invoice
        var (shopId, client, invoiceId) = await ArrangeInvoiceAsync();

        // Act — add a line
        var lineResp = await client.PostAsJsonAsync($"/api/billing/invoices/{invoiceId}/lines", new
        {
            ProductId            = 1L,
            ProductUnitId        = 1L,
            QuantityInBilledUnit = 2.0m,
            UnitPrice            = 500.0m,
            DiscountPercent      = 0.0m,
        });
        lineResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert — invoice totals recalculated (SubTotal = 2 × 500 = 1000)
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

        var invoice = await db.Set<Invoice>()
            .IgnoreQueryFilters()
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        invoice.Should().NotBeNull();
        invoice!.Lines.Should().HaveCount(1,
            "AddLine must create an InvoiceLine row linked to the invoice");
        invoice.SubTotal.Should().Be(1000.0m,
            "RecalculateTotals must update SubTotal = qty × unitPrice after AddLine");
        invoice.GrandTotal.Should().BeGreaterThan(0,
            "GrandTotal must be positive after adding a line with non-zero GST");
    }
}
