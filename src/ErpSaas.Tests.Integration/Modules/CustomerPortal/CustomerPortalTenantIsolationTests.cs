using ErpSaas.Tests.Integration.Fixtures;

namespace ErpSaas.Tests.Integration.Modules.CustomerPortal;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class CustomerPortalTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact(Skip = "TODO: Testcontainers gate — seed two shops, assert zero cross-shop leakage")]
    public async Task ListOnlineOrders_ShopA_CannotSeeShopBOrders()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "TODO: Testcontainers gate")]
    public async Task GetOrder_ShopA_CannotReadShopBOrder()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "TODO: Testcontainers gate")]
    public async Task ListInquiries_ShopA_CannotSeeShopBInquiries()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "TODO: Testcontainers gate")]
    public async Task CreateOrder_PortalCustomer_OrderScopedToCorrectShop()
    {
        await Task.CompletedTask;
    }
}
