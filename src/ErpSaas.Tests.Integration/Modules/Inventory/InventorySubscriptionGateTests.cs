// ── Inventory — Subscription Gate Tests ──────────────────────────────────────
// Inventory has no [RequireFeature] gates on core endpoints.
// Verifies that list endpoints return 200 regardless of the features claim.
// ─────────────────────────────────────────────────────────────────────────────

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Xunit;

namespace ErpSaas.Tests.Integration.Modules.Inventory;

[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Module", "Inventory")]
public class InventorySubscriptionGateTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task InventoryEndpoints_AllPlans_Returns200()
    {
        // Arrange: client with all permissions but no feature flags
        // (simulates a shop whose subscription has no feature gates)
        var client = fixture.CreateNoFeatureClient(shopId: 1);

        // Act: GET /api/inventory/products — no [RequireFeature] so must always succeed
        var response = await client.GetAsync("/api/inventory/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task InventoryWarehouses_AllPlans_Returns200()
    {
        var client = fixture.CreateNoFeatureClient(shopId: 1);

        var response = await client.GetAsync("/api/inventory/warehouses");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }
}
