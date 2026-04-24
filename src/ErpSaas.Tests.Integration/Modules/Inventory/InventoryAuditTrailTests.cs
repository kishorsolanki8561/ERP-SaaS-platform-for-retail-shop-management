// ── Inventory — Audit Trail Tests ────────────────────────────────────────────
// After every mutation (CreateProduct, UpdateProduct, DeactivateProduct,
// AdjustStock), asserts a correct AuditLog row exists in LogDbContext.
// TODO (Phase 1): implement bodies with Testcontainers + AuditLog assertions.
// ─────────────────────────────────────────────────────────────────────────────

namespace ErpSaas.Tests.Integration.Modules.Inventory;

[Trait("Category", "Integration")]
[Trait("Module", "Inventory")]
public class InventoryAuditTrailTests
{
    [Fact(Skip = "TODO Phase 1 — CreateProduct writes AuditLog row")]
    public Task CreateProduct_WritesAuditLog() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — UpdateProduct writes AuditLog row with old+new values")]
    public Task UpdateProduct_WritesAuditLog() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — DeactivateProduct writes AuditLog row")]
    public Task DeactivateProduct_WritesAuditLog() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — AdjustStock writes AuditLog row with quantity delta")]
    public Task AdjustStock_WritesAuditLog() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — CreateWarehouse writes AuditLog row")]
    public Task CreateWarehouse_WritesAuditLog() => Task.CompletedTask;
}
