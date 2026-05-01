namespace ErpSaas.Tests.Integration.Modules.Accounting;

/// <summary>
/// Verifies that Accounting data from Shop A is never visible to Shop B.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "TenantIsolation")]
public class AccountingTenantIsolationTests
{
    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ListAccounts_ShopA_DoesNotReturnShopBAccounts() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ListVouchers_ShopA_DoesNotReturnShopBVouchers() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task PostVoucher_ShopA_CannotPostShopBVoucher() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ListExpenses_ShopA_DoesNotReturnShopBExpenses() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CloseFinancialYear_ShopA_CannotCloseShopBFinancialYear() => await Task.CompletedTask;
}
