namespace ErpSaas.Tests.Integration.Modules.SalesReturns;

[Trait("Category", "Integration")]
[Trait("Category", "TenantIsolation")]
public class SalesReturnsTenantIsolationTests
{
    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ListSalesReturns_ShopA_DoesNotReturnShopBReturns() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ListCreditNotes_ShopA_DoesNotReturnShopBCreditNotes() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ApplyCreditNote_ShopA_CannotUseShopBCreditNote() => await Task.CompletedTask;
}
