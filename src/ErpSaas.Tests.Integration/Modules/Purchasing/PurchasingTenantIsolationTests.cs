namespace ErpSaas.Tests.Integration.Modules.Purchasing;

[Trait("Category", "Integration")]
[Trait("Category", "TenantIsolation")]
public class PurchasingTenantIsolationTests
{
    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ListSuppliers_ShopA_DoesNotReturnShopBSuppliers() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ListPurchaseOrders_ShopA_DoesNotReturnShopBOrders() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task ListBills_ShopA_DoesNotReturnShopBBills() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task CreatePurchaseOrder_ShopA_CannotUseShopBSupplier() => await Task.CompletedTask;

    [Fact(Skip = "Requires IntegrationTestFixture — Phase 2")]
    public async Task PayBill_ShopA_CannotPayShopBBill() => await Task.CompletedTask;
}
