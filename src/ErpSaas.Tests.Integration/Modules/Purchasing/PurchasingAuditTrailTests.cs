namespace ErpSaas.Tests.Integration.Modules.Purchasing;

[Trait("Category", "Integration")]
[Trait("Category", "AuditTrail")]
public class PurchasingAuditTrailTests
{
    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateSupplier_ProducesAuditLogRow() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreatePurchaseOrder_ProducesAuditLogRow() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ApproveBill_ProducesAuditLogRow() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task PayBill_ProducesAuditLogRow() => await Task.CompletedTask;
}
