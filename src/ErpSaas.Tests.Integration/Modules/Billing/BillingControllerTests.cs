using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Billing;

/// <summary>
/// Integration tests for <c>BillingController</c> exercised through the full
/// HTTP pipeline against a real SQL Server instance (Testcontainers).
///
/// Tests verify authentication, permission gating, request validation,
/// and the happy path for every controller action.
///
/// Prerequisites for invoice creation:
///   - Customer: POST /api/crm/customers
///   - Warehouse: POST /api/inventory/warehouses
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class BillingControllerTests(IntegrationTestFixture fixture)
{
    // ── Helper: creates a customer and warehouse, returns their IDs ────────────

    private async Task<(long customerId, long warehouseId, long shopId)> CreateInvoicePrereqsAsync()
    {
        var (shopId, _, _) = await fixture.SeedTestUserAsync();
        var client = fixture.CreateAuthenticatedClient(shopId: shopId, permissions: ["*"]);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        // Create customer
        var custResp = await client.PostAsJsonAsync("/api/crm/customers", new
        {
            DisplayName  = $"Test Customer {suffix}",
            CustomerType = "RETAIL",
            CreditLimit  = 0.0m,
        });
        custResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var customerId  = await custResp.Content.ReadFromJsonAsync<long>();

        // Create warehouse
        var whResp = await client.PostAsJsonAsync("/api/inventory/warehouses", new
        {
            Code      = $"WH-{suffix}",
            Name      = $"Test Warehouse {suffix}",
            IsDefault = false,
        });
        whResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var warehouseId = await whResp.Content.ReadFromJsonAsync<long>();

        return (customerId, warehouseId, shopId);
    }

    private async Task<long> CreateDraftInvoiceAsync(
        HttpClient client, long shopId, long customerId, long warehouseId)
    {
        var response = await client.PostAsJsonAsync("/api/billing/invoices", new
        {
            InvoiceDate  = DateTime.UtcNow,
            CustomerId   = customerId,
            WarehouseId  = warehouseId,
            ShopId       = shopId,
            Notes        = "Integration test invoice",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<long>();
    }

    // ── GET /api/billing/invoices ─────────────────────────────────────────────

    [Fact]
    public async Task ListInvoices_Unauthenticated_Returns401()
    {
        var client   = fixture.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/billing/invoices");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListInvoices_WithoutPermission_Returns403()
    {
        // CreateLimitedClient sends a JWT with a "None.None" permission — no Billing.View
        var client   = fixture.CreateLimitedClient(permissionCode: "None.None");
        var response = await client.GetAsync("/api/billing/invoices");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListInvoices_WithPermission_Returns200AndList()
    {
        var client   = fixture.CreateAuthenticatedClient(permissions: ["*"]);
        var response = await client.GetAsync("/api/billing/invoices");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Body is a PagedResult directly — verify the shape
        body.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
    }

    // ── POST /api/billing/invoices ────────────────────────────────────────────

    [Fact]
    public async Task CreateInvoice_ValidRequest_Returns200WithId()
    {
        var (customerId, warehouseId, shopId) = await CreateInvoicePrereqsAsync();
        var client = fixture.CreateAuthenticatedClient(shopId: shopId, permissions: ["*"]);

        var response = await client.PostAsJsonAsync("/api/billing/invoices", new
        {
            InvoiceDate  = DateTime.UtcNow,
            CustomerId   = customerId,
            WarehouseId  = warehouseId,
            ShopId       = shopId,
            Notes        = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<long>();
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateInvoice_WithoutCreatePermission_Returns403()
    {
        // A client with only "None.None" has no Billing.Create
        var client   = fixture.CreateLimitedClient(permissionCode: "None.None");
        var response = await client.PostAsJsonAsync("/api/billing/invoices", new
        {
            InvoiceDate  = DateTime.UtcNow,
            CustomerId   = 1L,
            WarehouseId  = 1L,
            ShopId       = 1L,
            Notes        = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/billing/invoices/{id}/lines ─────────────────────────────────

    [Fact]
    public async Task AddLine_InvoiceNotFound_Returns404()
    {
        // Using a non-existent invoice ID (99999) to verify 404 without complex setup
        var client   = fixture.CreateAuthenticatedClient(permissions: ["*"]);
        var response = await client.PostAsJsonAsync("/api/billing/invoices/99999/lines", new
        {
            ProductId            = 1L,
            ProductUnitId        = 1L,
            QuantityInBilledUnit = 1.0m,
            UnitPrice            = 100.0m,
            DiscountPercent      = 0.0m,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddLine_ValidRequest_Returns200()
    {
        // Create invoice first, then add a line
        // Note: product and unit records are not validated by name yet (service uses defaults).
        var (customerId, warehouseId, shopId) = await CreateInvoicePrereqsAsync();
        var client    = fixture.CreateAuthenticatedClient(shopId: shopId, permissions: ["*"]);
        var invoiceId = await CreateDraftInvoiceAsync(client, shopId, customerId, warehouseId);

        // Add line — BillingService uses "Pending" as product name snapshot until Inventory wired
        var response = await client.PostAsJsonAsync($"/api/billing/invoices/{invoiceId}/lines", new
        {
            ProductId            = 1L,
            ProductUnitId        = 1L,
            QuantityInBilledUnit = 2.0m,
            UnitPrice            = 250.0m,
            DiscountPercent      = 0.0m,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/billing/invoices/{id}/finalize ──────────────────────────────

    [Fact]
    public async Task FinalizeInvoice_DraftInvoice_Returns200()
    {
        var (customerId, warehouseId, shopId) = await CreateInvoicePrereqsAsync();
        var client    = fixture.CreateAuthenticatedClient(shopId: shopId, permissions: ["*"]);
        var invoiceId = await CreateDraftInvoiceAsync(client, shopId, customerId, warehouseId);

        var response = await client.PostAsync(
            $"/api/billing/invoices/{invoiceId}/finalize", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FinalizeInvoice_AlreadyFinalized_Returns409()
    {
        var (customerId, warehouseId, shopId) = await CreateInvoicePrereqsAsync();
        var client    = fixture.CreateAuthenticatedClient(shopId: shopId, permissions: ["*"]);
        var invoiceId = await CreateDraftInvoiceAsync(client, shopId, customerId, warehouseId);

        // First finalize — should succeed
        var first = await client.PostAsync(
            $"/api/billing/invoices/{invoiceId}/finalize", null);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second finalize — should conflict (already finalized)
        var second = await client.PostAsync(
            $"/api/billing/invoices/{invoiceId}/finalize", null);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── POST /api/billing/invoices/{id}/cancel ────────────────────────────────

    [Fact]
    public async Task CancelInvoice_WithoutCancelPermission_Returns403()
    {
        // A limited client has no Billing.Cancel permission
        var client   = fixture.CreateLimitedClient(permissionCode: "None.None");
        var response = await client.PostAsJsonAsync("/api/billing/invoices/1/cancel",
            new { Reason = "Test cancel" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CancelInvoice_ValidRequest_Returns200AndStatusCancelled()
    {
        var (customerId, warehouseId, shopId) = await CreateInvoicePrereqsAsync();
        var client    = fixture.CreateAuthenticatedClient(shopId: shopId, permissions: ["*"]);
        var invoiceId = await CreateDraftInvoiceAsync(client, shopId, customerId, warehouseId);

        var response = await client.PostAsJsonAsync(
            $"/api/billing/invoices/{invoiceId}/cancel",
            new { Reason = "Test cancellation" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify cancelled invoice cannot be retrieved as active
        var getResp = await client.GetAsync($"/api/billing/invoices/{invoiceId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var getBody = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        getBody.GetProperty("status").GetString()
            .Should().Be("Cancelled",
                "the invoice status must be Cancelled after a successful cancel request");
    }
}
