using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Wallet;

/// <summary>
/// Verifies subscription-gating behaviour for Wallet features.
///
/// When a feature flag (e.g. <c>Wallet.CustomerWallet</c>) is disabled for a
/// shop's subscription plan, the relevant endpoint must return HTTP 402 and the
/// menu item must be hidden.  When enabled, it must return 200.
///
/// Full implementation requires <c>IntegrationTestFixture</c> + subscription
/// plan seeding — deferred to Phase 1.
/// </summary>
[Trait("Category", "Integration")]
public class WalletSubscriptionGateTests
{
    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task WalletCredit_StarterPlan_Returns402()
    {
        // Customer wallet is a Growth+ feature.
        // Arrange: shop on Starter plan
        // Act: POST /api/wallet/credit
        // Assert: 402 Payment Required
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task WalletCredit_GrowthPlan_Returns200()
    {
        // Arrange: shop on Growth plan with Wallet.CustomerWallet feature flag enabled
        // Act: POST /api/wallet/credit
        // Assert: 200
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task WalletDebit_StarterPlan_Returns402()
    {
        // Arrange: shop on Starter plan
        // Act: POST /api/wallet/debit
        // Assert: 402
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task WalletMenuItems_GrowthPlan_AllVisible()
    {
        // Wallet menu items should appear for Growth+ plans.
        // Arrange: shop on Growth plan
        // Assert: wallet menu items present in menu tree response
        await Task.CompletedTask;
    }
}
