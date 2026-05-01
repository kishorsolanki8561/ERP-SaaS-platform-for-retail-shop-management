namespace ErpSaas.Tests.Integration.Modules.Reports;

[Trait("Category", "Integration")]
public sealed class ReportsSubscriptionGateTests
{
    [Fact(Skip = "Phase 2 integration stubs — implement after containers wired")]
    public Task Reports_FeatureOff_Returns402() => Task.CompletedTask;

    [Fact(Skip = "Phase 2 integration stubs — implement after containers wired")]
    public Task Reports_FeatureOn_Returns200() => Task.CompletedTask;
}
