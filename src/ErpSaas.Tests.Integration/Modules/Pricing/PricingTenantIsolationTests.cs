namespace ErpSaas.Tests.Integration.Modules.Pricing;

// TODO: Implement tenant isolation tests — seed Shop A + Shop B, assert no cross-shop reads (Phase 3 cleanup)
[Trait("Category", "Integration")]
public sealed class PricingTenantIsolationTests
{
    [Fact(Skip = "Stub — implement in Phase 3 cleanup")]
    public Task Placeholder() => Task.CompletedTask;
}
