using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Billing;

/// <summary>
/// Verifies that invoices created in Shop A are never visible to Shop B,
/// and that mutations from Shop B cannot affect Shop A's invoices.
///
/// These tests exercise the global query filter on <c>TenantEntity.ShopId</c>
/// which is enforced by <c>TenantSaveChangesInterceptor</c> and EF query
/// filters (§4.4 of CLAUDE.md).
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class BillingTenantIsolationTests(IntegrationTestFixture fixture)
{
    // ── Shared helpers ────────────────────────────────────────────────────────

    private async Task<(long shopId, HttpClient client, long invoiceId)> CreateShopWithInvoiceAsync()
    {
        var (shopId, _, _) = await fixture.SeedTestUserAsync();
        var client  = fixture.CreateAuthenticatedClient(shopId: shopId, permissions: ["*"]);
        var suffix  = Guid.NewGuid().ToString("N")[..8];

        // Create customer
        var custResp = await client.PostAsJsonAsync("/api/crm/customers", new
        {
            DisplayName  = $"Iso Customer {suffix}",
            CustomerType = "RETAIL",
            CreditLimit  = 0.0m,
        });
        custResp.StatusCode.Should().Be(HttpStatusCode.OK);
        // All Create endpoints return Result<long> → OkObjectResult(id) → plain long.
        var customerId = await custResp.Content.ReadFromJsonAsync<long>();

        // Create warehouse
        var whResp = await client.PostAsJsonAsync("/api/inventory/warehouses", new
        {
            Code      = $"WH-ISO-{suffix}",
            Name      = $"Iso Warehouse {suffix}",
            IsDefault = false,
        });
        whResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var warehouseId = await whResp.Content.ReadFromJsonAsync<long>();

        // Create invoice
        var invResp = await client.PostAsJsonAsync("/api/billing/invoices", new
        {
            InvoiceDate = DateTime.UtcNow,
            CustomerId  = customerId,
            WarehouseId = warehouseId,
            ShopId      = shopId,
            Notes       = $"Isolation test invoice {suffix}",
        });
        invResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var invoiceId = await invResp.Content.ReadFromJsonAsync<long>();

        return (shopId, client, invoiceId);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListInvoices_ShopA_DoesNotReturnShopBInvoices()
    {
        // Arrange — create an invoice as Shop A and an invoice as Shop B
        var (shopAId, clientA, shopAInvoiceId) = await CreateShopWithInvoiceAsync();
        var (shopBId, _, shopBInvoiceId)        = await CreateShopWithInvoiceAsync();

        // Act — list invoices as Shop A
        var response = await clientA.GetAsync("/api/billing/invoices");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // ListInvoicesAsync returns PagedResult<T> → OkObjectResult(pagedResult) → {items, total, ...}
        var body  = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("items");
        var ids   = items.EnumerateArray()
            .Select(i => i.GetProperty("id").GetInt64())
            .ToList();

        // Assert — Shop B's invoice must not appear
        ids.Should().Contain(shopAInvoiceId,
            "Shop A's own invoice must be visible to Shop A");
        ids.Should().NotContain(shopBInvoiceId,
            "Shop B's invoice must never be visible to Shop A");
    }

    [Fact]
    public async Task GetInvoice_ShopBCannotReadShopAInvoice_Returns404()
    {
        // Arrange — create invoice in Shop A
        var (shopAId, _, shopAInvoiceId) = await CreateShopWithInvoiceAsync();
        var (shopBId, _, _)              = await CreateShopWithInvoiceAsync();

        // Act — try to GET Shop A's invoice as Shop B
        var clientB  = fixture.CreateAuthenticatedClient(shopId: shopBId, permissions: ["*"]);
        var response = await clientB.GetAsync($"/api/billing/invoices/{shopAInvoiceId}");

        // Assert — 404 because the global query filter excludes Shop A's rows from Shop B's context
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Shop B must not be able to read Shop A's invoices — global query filter enforces isolation");
    }

    [Fact]
    public async Task CancelInvoice_ShopBCannotCancelShopAInvoice_Returns404()
    {
        // Arrange — create draft invoice in Shop A
        var (shopAId, _, shopAInvoiceId) = await CreateShopWithInvoiceAsync();
        var (shopBId, _, _)              = await CreateShopWithInvoiceAsync();

        // Act — try to cancel Shop A's invoice as Shop B
        var clientB  = fixture.CreateAuthenticatedClient(shopId: shopBId, permissions: ["*"]);
        var response = await clientB.PostAsJsonAsync(
            $"/api/billing/invoices/{shopAInvoiceId}/cancel",
            new { Reason = "Cross-tenant attack" });

        // Assert — 404 (not 403) to avoid leaking existence of the record
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Shop B must receive 404 (not 403) to avoid leaking Shop A's invoice existence");
    }

    [Fact]
    public async Task AddLine_ShopBCannotAddLineToShopAInvoice_Returns404()
    {
        // Arrange — create draft invoice in Shop A
        var (shopAId, _, shopAInvoiceId) = await CreateShopWithInvoiceAsync();
        var (shopBId, _, _)              = await CreateShopWithInvoiceAsync();

        // Act — try to add a line to Shop A's invoice as Shop B
        var clientB  = fixture.CreateAuthenticatedClient(shopId: shopBId, permissions: ["*"]);
        var response = await clientB.PostAsJsonAsync(
            $"/api/billing/invoices/{shopAInvoiceId}/lines",
            new
            {
                ProductId            = 1L,
                ProductUnitId        = 1L,
                QuantityInBilledUnit = 1.0m,
                UnitPrice            = 100.0m,
                DiscountPercent      = 0.0m,
            });

        // Assert — 404 due to tenant isolation
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Shop B must not be able to add lines to Shop A's invoices");
    }
}
