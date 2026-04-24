using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Billing;

/// <summary>
/// Verifies subscription-gating behaviour for Billing features.
///
/// When a feature flag (e.g. <c>Billing.EInvoice</c>) is disabled for a shop's
/// subscription plan, the relevant endpoint must return HTTP 402 and the menu
/// item must be hidden.  When enabled, it must return 200.
///
/// Full implementation requires <c>IntegrationTestFixture</c> + subscription
/// plan seeding — deferred to Phase 1.
/// </summary>
[Trait("Category", "Integration")]
public class BillingSubscriptionGateTests
{
    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task BillingInvoicing_AllPlans_Returns200()
    {
        // Core invoicing is available on all plans.
        // Arrange: shop on Starter plan, user has Billing.Create
        // Act: POST /api/billing/invoices
        // Assert: 200
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task EInvoice_StarterPlan_Returns402()
    {
        // E-Invoice (IRN generation) is a Growth+ feature.
        // Arrange: shop on Starter plan
        // Act: POST /api/billing/invoices/{id}/e-invoice
        // Assert: 402 Payment Required
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task EInvoice_GrowthPlan_Returns200()
    {
        // Arrange: shop on Growth plan with Billing.EInvoice feature flag enabled
        // Act: POST /api/billing/invoices/{id}/e-invoice
        // Assert: 200
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task BillingMenuItems_StarterPlan_AllVisible()
    {
        // Core billing menu items should appear for all plans.
        await Task.CompletedTask;
    }
}
