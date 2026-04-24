// ── Inventory — Subscription Gate Tests ──────────────────────────────────────
// Verifies that feature-gated Inventory endpoints return 402 when the shop's
// subscription plan does not include the required feature, and 200 when it does.
// Also verifies menu items are hidden when the feature is disabled.
// TODO (Phase 1): implement bodies with SubscriptionPlan fixture + WAF.
// ─────────────────────────────────────────────────────────────────────────────

namespace ErpSaas.Tests.Integration.Modules.Inventory;

[Trait("Category", "Integration")]
[Trait("Module", "Inventory")]
public class InventorySubscriptionGateTests
{
    [Fact(Skip = "TODO Phase 1 — feature off → 402 on product list")]
    public Task ListProducts_FeatureDisabled_Returns402() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — feature on → 200 on product list")]
    public Task ListProducts_FeatureEnabled_Returns200() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — feature off → menu item hidden")]
    public Task MenuTree_FeatureDisabled_HidesInventoryMenu() => Task.CompletedTask;

    [Fact(Skip = "TODO Phase 1 — feature on → menu item visible")]
    public Task MenuTree_FeatureEnabled_ShowsInventoryMenu() => Task.CompletedTask;
}
