namespace ErpSaas.Tests.Integration.Modules.SalesReturns;

[Trait("Category", "Integration")]
[Trait("Category", "AuditTrail")]
public class SalesReturnsAuditTrailTests
{
    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateSalesReturn_ProducesAuditLogRow() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ApproveSalesReturn_ProducesAuditLogRow() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task IssueCreditNote_ProducesAuditLogRow() => await Task.CompletedTask;
}
