namespace ErpSaas.Tests.Integration.Modules.SalesReturns;

[Trait("Category", "Integration")]
public class SalesReturnsControllerTests
{
    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateSalesReturn_ValidRequest_Returns200WithId() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateSalesReturn_WithoutPermission_Returns403() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ApproveSalesReturn_DraftReturn_Returns200() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ApproveSalesReturn_AlreadyApproved_Returns409() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task IssueCreditNote_ValidRequest_Returns200WithId() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ApplyCreditNote_ValidAmount_Returns200() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ApplyCreditNote_OverAmount_Returns409() => await Task.CompletedTask;
}
