using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Shift;

/// <summary>
/// Verifies subscription-gating behaviour for Shift (POS cash management) features.
///
/// When a feature flag (e.g. <c>Shift.CashManagement</c>) is disabled for a
/// shop's subscription plan, the relevant endpoint must return HTTP 402 and the
/// menu item must be hidden.  When enabled, it must return 200.
///
/// Full implementation requires <c>IntegrationTestFixture</c> + subscription
/// plan seeding — deferred to Phase 1.
/// </summary>
[Trait("Category", "Integration")]
public class ShiftSubscriptionGateTests
{
    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task OpenShift_AllPlans_Returns200()
    {
        // Core shift management is available on all plans.
        // Arrange: shop on Starter plan, user has Shift.Open
        // Act: POST /api/shift/open
        // Assert: 200
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task ShiftDenominations_StarterPlan_Returns402()
    {
        // Denomination tracking is a Growth+ feature.
        // Arrange: shop on Starter plan
        // Act: POST /api/shift/open with Denominations array
        // Assert: 402 Payment Required
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task ShiftDenominations_GrowthPlan_Returns200()
    {
        // Arrange: shop on Growth plan with Shift.Denominations feature enabled
        // Act: POST /api/shift/open with Denominations
        // Assert: 200
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task ShiftMenuItems_AllPlans_AllVisible()
    {
        // Core shift menu items should appear for all plans.
        await Task.CompletedTask;
    }
}
