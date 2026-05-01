namespace ErpSaas.Tests.Integration.Modules.Accounting;

/// <summary>
/// Verifies that every mutating Accounting operation produces a correct AuditLog row.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "AuditTrail")]
public class AccountingAuditTrailTests
{
    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateAccount_ProducesAuditLogRow() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task PostVoucher_ProducesAuditLogRow() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ReverseVoucher_ProducesAuditLogRow() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CloseFinancialYear_ProducesAuditLogRow() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreateExpense_ProducesAuditLogRow() => await Task.CompletedTask;
}
