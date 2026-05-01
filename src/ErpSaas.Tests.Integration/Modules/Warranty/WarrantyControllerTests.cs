namespace ErpSaas.Tests.Integration.Modules.Warranty;

[Trait("Category", "Integration")]
public sealed class WarrantyControllerTests
{
    [Fact(Skip = "Phase 3 integration stubs — implement after containers wired")]
    public Task GetBySerial_WithoutAuth_Returns401() => Task.CompletedTask;

    [Fact(Skip = "Phase 3 integration stubs — implement after containers wired")]
    public Task RegisterWarranty_HappyPath_Returns200() => Task.CompletedTask;

    [Fact(Skip = "Phase 3 integration stubs — implement after containers wired")]
    public Task CreateClaim_ExpiredWarranty_Returns409() => Task.CompletedTask;
}
