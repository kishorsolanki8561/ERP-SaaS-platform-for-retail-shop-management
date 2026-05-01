namespace ErpSaas.Tests.Integration.Modules.Purchasing;

[Trait("Category", "Integration")]
[Trait("Category", "SubscriptionGate")]
public class PurchasingSubscriptionGateTests
{
    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task PurchasingEndpoints_StarterPlan_AllAccessible() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task PurchasingMenuItems_VisibleWhenPermissionGranted() => await Task.CompletedTask;
}
