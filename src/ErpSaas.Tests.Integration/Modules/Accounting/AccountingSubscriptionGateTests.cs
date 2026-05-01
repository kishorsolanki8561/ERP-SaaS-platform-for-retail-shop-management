namespace ErpSaas.Tests.Integration.Modules.Accounting;

/// <summary>
/// Verifies that feature-gated Accounting endpoints return 402 when the shop's plan
/// does not include the required feature, and 200 when it does.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "SubscriptionGate")]
public class AccountingSubscriptionGateTests
{
    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CloseFinancialYear_StarterPlan_Returns402() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CloseFinancialYear_GrowthPlan_Returns200() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task AccountingMenuItems_HiddenWhenFeatureOff() => await Task.CompletedTask;
}
