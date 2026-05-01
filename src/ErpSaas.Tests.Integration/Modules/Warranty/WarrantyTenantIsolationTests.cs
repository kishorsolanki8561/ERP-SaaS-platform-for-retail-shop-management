namespace ErpSaas.Tests.Integration.Modules.Warranty;

[Trait("Category", "Integration")]
public sealed class WarrantyTenantIsolationTests
{
    [Fact(Skip = "Phase 3 integration stubs — implement after containers wired")]
    public Task Registrations_Shop1Token_ReturnsOnlyShop1Data() => Task.CompletedTask;

    [Fact(Skip = "Phase 3 integration stubs — implement after containers wired")]
    public Task Claims_Shop1Token_ReturnsOnlyShop1Data() => Task.CompletedTask;
}
