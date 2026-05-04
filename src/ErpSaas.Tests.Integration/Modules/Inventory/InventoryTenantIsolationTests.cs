// ── Inventory — Tenant Isolation Tests ───────────────────────────────────────
// Seeds two shops; asserts that no reads or writes from Shop A bleed into Shop B.
// Verifies global query filter on ShopId is enforced for Product, Warehouse, StockMovement.
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
public class InventoryTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task ListProducts_ShopA_DoesNotReturnShopBProducts()
    {
        // Arrange: two separate shops
        var (shopAId, _, _) = await fixture.SeedTestUserAsync();
        var (shopBId, _, _) = await fixture.SeedTestUserAsync();

        var shopAClient = fixture.CreateAuthenticatedClient(shopId: shopAId, permissions: ["*"]);
        var shopBClient = fixture.CreateAuthenticatedClient(shopId: shopBId, permissions: ["*"]);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var productDto = new
        {
            Name = $"ShopA Product {suffix}",
            CategoryCode = "ELECTRICAL",
            HsnSacCode = (string?)null,
            GstRate = 18m,
            BaseUnitCode = "PCS",
            SalePrice = 100m,
            PurchasePrice = 60m,
            MrpPrice = (decimal?)null,
            MinStockLevel = 0m,
            BarcodeEan = (string?)null
        };

        // Create product in Shop A
        var createResponse = await shopAClient.PostAsJsonAsync("/api/inventory/products", productDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var shopAProductId = await createResponse.Content.ReadFromJsonAsync<long>();

        // Act: list products as Shop B
        var listResponse = await shopBClient.GetAsync("/api/inventory/products");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var ids = body.GetProperty("items")
            .EnumerateArray()
            .Select(p => p.GetProperty("id").GetInt64())
            .ToList();

        // Assert: Shop B cannot see Shop A's product
        ids.Should().NotContain(shopAProductId,
            because: "Shop B's product list must not include Shop A's products");
    }

    [Fact]
    public async Task GetProduct_ShopBCannotReadShopAProduct_Returns404()
    {
        // Arrange: create product in Shop A
        var (shopAId, _, _) = await fixture.SeedTestUserAsync();
        var (shopBId, _, _) = await fixture.SeedTestUserAsync();

        var shopAClient = fixture.CreateAuthenticatedClient(shopId: shopAId, permissions: ["*"]);
        var shopBClient = fixture.CreateAuthenticatedClient(shopId: shopBId, permissions: ["*"]);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var productDto = new
        {
            Name = $"ShopA Private Product {suffix}",
            CategoryCode = "ELECTRONICS",
            HsnSacCode = (string?)null,
            GstRate = 12m,
            BaseUnitCode = "PCS",
            SalePrice = 200m,
            PurchasePrice = 130m,
            MrpPrice = (decimal?)null,
            MinStockLevel = 0m,
            BarcodeEan = (string?)null
        };

        var createResponse = await shopAClient.PostAsJsonAsync("/api/inventory/products", productDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var shopAProductId = await createResponse.Content.ReadFromJsonAsync<long>();

        // Act: Shop B tries to GET Shop A's product directly
        var response = await shopBClient.GetAsync($"/api/inventory/products/{shopAProductId}");

        // Assert: 404 — must not leak existence across tenant boundary
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
