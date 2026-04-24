// ── Inventory — Controller Integration Tests ──────────────────────────────────
// Covers: every endpoint — auth, permission gates, validation, 200/400/401/403/404.
// Uses: Testcontainers SQL Server + WebApplicationFactory.
// TODO (Phase 1): implement test bodies; stub compiles and is skipped at runtime.
// ─────────────────────────────────────────────────────────────────────────────

using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Inventory;

[Trait("Category", "Integration")]
[Trait("Module", "Inventory")]
public class InventoryControllerTests
{
    // ── Products ──────────────────────────────────────────────────────────────

    [Fact(Skip = "TODO Phase 1 — implement with WAF + Testcontainers")]
    public Task ListProducts_Unauthenticated_Returns401() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — implement with WAF + Testcontainers")]
    public Task ListProducts_MissingPermission_Returns403() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — implement with WAF + Testcontainers")]
    public Task ListProducts_Authenticated_Returns200WithPagedList() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — implement with WAF + Testcontainers")]
    public Task GetProduct_UnknownId_Returns404() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — implement with WAF + Testcontainers")]
    public Task CreateProduct_InvalidDto_Returns422() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — implement with WAF + Testcontainers")]
    public Task CreateProduct_ValidDto_Returns200WithId() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — implement with WAF + Testcontainers")]
    public Task UpdateProduct_UnknownId_Returns404() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — implement with WAF + Testcontainers")]
    public Task UpdateProduct_ValidDto_Returns200() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — implement with WAF + Testcontainers")]
    public Task DeactivateProduct_UnknownId_Returns404() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — implement with WAF + Testcontainers")]
    public Task DeactivateProduct_ExistingId_Returns200() => Task.CompletedTask;

    // ── Warehouses ────────────────────────────────────────────────────────────

    [Fact(Skip = "TODO Phase 1 — implement with WAF + Testcontainers")]
    public Task CreateWarehouse_DuplicateCode_Returns409() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — implement with WAF + Testcontainers")]
    public Task CreateWarehouse_Valid_Returns200WithId() => Task.CompletedTask;

    // ── Stock ─────────────────────────────────────────────────────────────────

    [Fact(Skip = "TODO Phase 1 — implement with WAF + Testcontainers")]
    public Task GetStockLevel_Returns200WithBalance() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — implement with WAF + Testcontainers")]
    public Task AdjustStock_UnknownUnit_Returns404() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — implement with WAF + Testcontainers")]
    public Task AdjustStock_ValidDto_CalculatesBaseUnitQuantity() => Task.CompletedTask;
}
