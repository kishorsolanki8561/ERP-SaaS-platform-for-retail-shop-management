// ── Inventory — Controller Integration Tests ──────────────────────────────────
// Covers: every endpoint — auth, permission gates, validation, 200/400/401/403/404.
// Uses: Testcontainers SQL Server + WebApplicationFactory.
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
public class InventoryControllerTests(IntegrationTestFixture fixture)
{
    // ── Products ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListProducts_Unauthenticated_Returns401()
    {
        var client = fixture.CreateUnauthenticatedClient();

        var response = await client.GetAsync("/api/inventory/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListProducts_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(shopId: 1, permissionCode: "None.None");

        var response = await client.GetAsync("/api/inventory/products");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListProducts_Authenticated_Returns200WithPagedList()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["Inventory.View"]);

        var response = await client.GetAsync("/api/inventory/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
        body.TryGetProperty("totalCount", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetProduct_UnknownId_Returns404()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["Inventory.View"]);

        var response = await client.GetAsync("/api/inventory/products/999999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_ValidDto_Returns200WithId()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["*"]);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var dto = new
        {
            Name = $"Test Product {suffix}",
            CategoryCode = "ELECTRICAL",
            HsnSacCode = (string?)null,
            GstRate = 18m,
            BaseUnitCode = "PCS",
            SalePrice = 150m,
            PurchasePrice = 100m,
            MrpPrice = (decimal?)null,
            MinStockLevel = 0m,
            BarcodeEan = (string?)null
        };

        var response = await client.PostAsJsonAsync("/api/inventory/products", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<long>();
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateProduct_InvalidDto_Returns422()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["*"]);

        // Send an empty body — required fields Name, CategoryCode, BaseUnitCode are missing
        var response = await client.PostAsJsonAsync("/api/inventory/products", new { });

        // FluentValidation or model binding returns 422 or 400 for invalid payloads
        ((int)response.StatusCode).Should().BeOneOf(400, 422);
    }

    [Fact]
    public async Task UpdateProduct_UnknownId_Returns404()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["*"]);
        var dto = new
        {
            Name = "Updated Name",
            Description = (string?)null,
            CategoryCode = "ELECTRICAL",
            GstRate = 18m,
            SalePrice = 200m,
            PurchasePrice = 120m,
            MrpPrice = (decimal?)null,
            MinStockLevel = 0m
        };

        var response = await client.PutAsJsonAsync("/api/inventory/products/999999999", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeactivateProduct_UnknownId_Returns404()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["*"]);

        var response = await client.DeleteAsync("/api/inventory/products/999999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Warehouses ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateWarehouse_Valid_Returns200WithId()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["*"]);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new
        {
            Code = $"WH-{suffix}",
            Name = $"Test Warehouse {suffix}",
            IsDefault = false
        };

        var response = await client.PostAsJsonAsync("/api/inventory/warehouses", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<long>();
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateWarehouse_DuplicateCode_Returns409()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["*"]);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var code = $"WH-DUP-{suffix}";
        var request = new
        {
            Code = code,
            Name = $"Warehouse First {suffix}",
            IsDefault = false
        };

        // First creation — should succeed
        var first = await client.PostAsJsonAsync("/api/inventory/warehouses", request);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second creation with same Code — must conflict
        var duplicate = new
        {
            Code = code,
            Name = $"Warehouse Duplicate {suffix}",
            IsDefault = false
        };
        var second = await client.PostAsJsonAsync("/api/inventory/warehouses", duplicate);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── Stock ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockLevel_Returns200WithBalance()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["*"]);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        // Create a product
        var productDto = new
        {
            Name = $"Stock Test Product {suffix}",
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
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products", productDto);
        productResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var productId = await productResponse.Content.ReadFromJsonAsync<long>();

        // Create a warehouse
        var warehouseRequest = new
        {
            Code = $"WH-STOCK-{suffix}",
            Name = $"Stock Test WH {suffix}",
            IsDefault = false
        };
        var warehouseResponse = await client.PostAsJsonAsync("/api/inventory/warehouses", warehouseRequest);
        warehouseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var warehouseId = await warehouseResponse.Content.ReadFromJsonAsync<long>();

        // Act: get stock level (should be zero since no movements yet)
        var response = await client.GetAsync($"/api/inventory/stock/{productId}/{warehouseId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("stockLevel", out _).Should().BeTrue();
    }

    [Fact]
    public async Task AdjustStock_UnknownUnit_Returns404()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["*"]);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        // Create a product and warehouse first
        var productDto = new
        {
            Name = $"Adj Unit Test Product {suffix}",
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
        var productResponse = await client.PostAsJsonAsync("/api/inventory/products", productDto);
        productResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var productId = await productResponse.Content.ReadFromJsonAsync<long>();

        var warehouseRequest = new
        {
            Code = $"WH-ADJUNIT-{suffix}",
            Name = $"Adj Unit WH {suffix}",
            IsDefault = false
        };
        var warehouseResponse = await client.PostAsJsonAsync("/api/inventory/warehouses", warehouseRequest);
        warehouseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var warehouseId = await warehouseResponse.Content.ReadFromJsonAsync<long>();

        // Act: adjust stock using a ProductUnitId that does not exist
        var adjustDto = new
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            ProductUnitId = 99999L,
            QuantityInBilledUnit = 10m,
            MovementType = "Adjustment",
            Remarks = (string?)null
        };
        var response = await client.PostAsJsonAsync("/api/inventory/stock/adjust", adjustDto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
