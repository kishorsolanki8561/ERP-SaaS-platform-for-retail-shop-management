// ── Inventory — Tenant Isolation Tests ───────────────────────────────────────
// Seeds two shops; asserts that no reads or writes from Shop A bleed into Shop B.
// Verifies global query filter on ShopId is enforced for Product, Warehouse, StockMovement.
// TODO (Phase 1): implement bodies with OnboardedShopFixture + Testcontainers.
// ─────────────────────────────────────────────────────────────────────────────

namespace ErpSaas.Tests.Integration.Modules.Inventory;

[Trait("Category", "Integration")]
[Trait("Module", "Inventory")]
public class InventoryTenantIsolationTests
{
    [Fact(Skip = "TODO Phase 1 — seed two shops, assert zero cross-shop product reads")]
    public Task ListProducts_ShopA_DoesNotReturnShopBProducts() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — stock movements for Shop A invisible to Shop B")]
    public Task GetStockLevel_ShopA_DoesNotSeeShopBMovements() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — warehouse list scoped to requesting shop")]
    public Task ListWarehouses_ShopA_DoesNotReturnShopBWarehouses() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — direct DB insert for Shop B invisible via service")]
    public Task CreateProduct_ShopA_NotAccessibleByShopB() => Task.CompletedTask;
}
