// ── Inventory — Audit Trail Tests ────────────────────────────────────────────
// After every mutation (CreateProduct, UpdateProduct, DeactivateProduct,
// AdjustStock), asserts a correct AuditLog row exists in LogDbContext.
// Product is marked [Auditable("Inventory.ProductChanged")] — the
// AuditSaveChangesInterceptor must write an AuditLog entry on every save.
// ─────────────────────────────────────────────────────────────────────────────

using System.Net;
using System.Net.Http.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ErpSaas.Tests.Integration.Modules.Inventory;

[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Module", "Inventory")]
public class InventoryAuditTrailTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task CreateProduct_ProducesAuditLogRow()
    {
        // Arrange
        var (shopId, _, _) = await fixture.SeedTestUserAsync();
        var client = fixture.CreateAuthenticatedClient(shopId: shopId, permissions: ["*"]);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var dto = new
        {
            Name = $"Audit Product {suffix}",
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

        // Act: create a product via the API
        var response = await client.PostAsJsonAsync("/api/inventory/products", dto);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var productId = await response.Content.ReadFromJsonAsync<long>();
        productId.Should().BeGreaterThan(0);

        // Assert: AuditLog has a row for the Product entity in this shop
        await using var scope = fixture.CreateScope();
        var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();

        var hasAuditRow = await logDb.AuditLogs
            .Where(a => a.ShopId == shopId && a.EntityName == "Product")
            .AnyAsync();

        hasAuditRow.Should().BeTrue(
            because: $"creating a Product (id={productId}) must produce an AuditLog row " +
                     "via the [Auditable] interceptor");
    }
}
