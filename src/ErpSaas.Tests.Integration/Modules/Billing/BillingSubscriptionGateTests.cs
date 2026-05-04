using System.Net;
using System.Net.Http.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Billing;

/// <summary>
/// Verifies subscription-gating behaviour for Billing features.
///
/// Core invoicing endpoints (<c>GET /api/billing/invoices</c>,
/// <c>POST /api/billing/invoices</c>, finalize, cancel) carry NO
/// <c>[RequireFeature]</c> attribute — they are available on all plans.
///
/// The only feature-gated billing endpoint is E-Invoice (IRN generation),
/// which is planned for a later phase.  These tests confirm that removing
/// the "feats" claim from the JWT does NOT gate any core billing endpoint.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class BillingSubscriptionGateTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task ListInvoices_NoFeaturesClaim_Returns200()
    {
        // CreateNoFeatureClient = is_platform_admin=true but NO feats claim.
        // Core list endpoint has no [RequireFeature] so must still return 200.
        var client   = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/billing/invoices");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "GET /api/billing/invoices has no [RequireFeature] and must work on all plans");
    }

    [Fact]
    public async Task BillingInvoicing_AllPlans_Returns200()
    {
        // Core invoicing — create customer + warehouse then invoice to confirm end-to-end.
        var (shopId, _, _) = await fixture.SeedTestUserAsync();
        var suffix  = Guid.NewGuid().ToString("N")[..8];

        // CreateNoFeatureClient: all permissions but no feats claim.
        var client = fixture.CreateNoFeatureClient(shopId: shopId);

        // Customer
        var custResp = await client.PostAsJsonAsync("/api/crm/customers", new
        {
            DisplayName  = $"SubGate Customer {suffix}",
            CustomerType = "RETAIL",
            CreditLimit  = 0.0m,
        });
        custResp.StatusCode.Should().Be(HttpStatusCode.OK);
        // All Create endpoints return Result<long> → OkObjectResult(id) → plain long.
        var customerId = await custResp.Content.ReadFromJsonAsync<long>();

        // Warehouse
        var whResp = await client.PostAsJsonAsync("/api/inventory/warehouses", new
        {
            Code      = $"WH-SG-{suffix}",
            Name      = $"SubGate WH {suffix}",
            IsDefault = false,
        });
        whResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var warehouseId = await whResp.Content.ReadFromJsonAsync<long>();

        // Create invoice — no feats claim must not block this
        var invResp = await client.PostAsJsonAsync("/api/billing/invoices", new
        {
            InvoiceDate = DateTime.UtcNow,
            CustomerId  = customerId,
            WarehouseId = warehouseId,
            ShopId      = shopId,
            Notes       = "Subscription gate test",
        });

        invResp.StatusCode.Should().Be(HttpStatusCode.OK,
            "POST /api/billing/invoices has no [RequireFeature] — must work with no feats claim");
    }

    [Fact]
    public async Task FinalizeInvoice_NoFeaturesClaim_Returns200()
    {
        // Arrange — create invoice (uses full-permission admin client to seed prerequistes)
        var (shopId, _, _) = await fixture.SeedTestUserAsync();
        var suffix  = Guid.NewGuid().ToString("N")[..8];
        var adminClient = fixture.CreateAuthenticatedClient(shopId: shopId, permissions: ["*"]);

        var custResp = await adminClient.PostAsJsonAsync("/api/crm/customers", new
        {
            DisplayName  = $"FinalSG Customer {suffix}",
            CustomerType = "RETAIL",
            CreditLimit  = 0.0m,
        });
        custResp.StatusCode.Should().Be(HttpStatusCode.OK);
        // All Create endpoints return Result<long> → OkObjectResult(id) → plain long.
        var customerId = await custResp.Content.ReadFromJsonAsync<long>();

        var whResp = await adminClient.PostAsJsonAsync("/api/inventory/warehouses", new
        {
            Code      = $"WH-FSG-{suffix}",
            Name      = $"FinalSG WH {suffix}",
            IsDefault = false,
        });
        whResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var warehouseId = await whResp.Content.ReadFromJsonAsync<long>();

        var invResp = await adminClient.PostAsJsonAsync("/api/billing/invoices", new
        {
            InvoiceDate = DateTime.UtcNow,
            CustomerId  = customerId,
            WarehouseId = warehouseId,
            ShopId      = shopId,
            Notes       = (string?)null,
        });
        invResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var invoiceId = await invResp.Content.ReadFromJsonAsync<long>();

        // Act — finalize using no-feature client
        var noFeatClient = fixture.CreateNoFeatureClient(shopId: shopId);
        var finalResp    = await noFeatClient.PostAsync(
            $"/api/billing/invoices/{invoiceId}/finalize", null);

        // Assert — finalize has no [RequireFeature] so must succeed
        finalResp.StatusCode.Should().Be(HttpStatusCode.OK,
            "POST /api/billing/invoices/{id}/finalize has no [RequireFeature]");
    }

    [Fact]
    public async Task BillingMenuItems_StarterPlan_AllVisible()
    {
        // Core billing menu items must not be gated by any feature flag.
        // This test validates the negative: a no-feature JWT does NOT produce
        // 402 on core billing endpoints — proxy for "menu item would be visible".
        var client   = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/billing/invoices");

        response.StatusCode.Should().NotBe(HttpStatusCode.PaymentRequired,
            "Core billing list endpoint must not return 402 on any subscription plan");
    }
}
