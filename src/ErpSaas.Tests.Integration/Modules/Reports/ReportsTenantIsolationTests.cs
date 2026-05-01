namespace ErpSaas.Tests.Integration.Modules.Reports;

[Trait("Category", "Integration")]
public sealed class ReportsTenantIsolationTests
{
    [Fact(Skip = "Phase 2 integration stubs — implement after containers wired")]
    public Task TrialBalance_Shop1Token_ReturnsOnlyShop1Vouchers() => Task.CompletedTask;

    [Fact(Skip = "Phase 2 integration stubs — implement after containers wired")]
    public Task GstR1_Shop1Token_ReturnsOnlyShop1Invoices() => Task.CompletedTask;
}
