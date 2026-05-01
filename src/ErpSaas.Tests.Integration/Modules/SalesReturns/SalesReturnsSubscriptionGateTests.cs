namespace ErpSaas.Tests.Integration.Modules.SalesReturns;

[Trait("Category", "Integration")]
[Trait("Category", "SubscriptionGate")]
public class SalesReturnsSubscriptionGateTests
{
    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task SalesReturnsEndpoints_AllPlans_AreAccessible() => await Task.CompletedTask;
}
