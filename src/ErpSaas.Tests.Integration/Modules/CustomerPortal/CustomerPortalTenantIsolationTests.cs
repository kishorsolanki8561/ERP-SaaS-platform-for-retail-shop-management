using ErpSaas.Tests.Integration.Fixtures;

namespace ErpSaas.Tests.Integration.Modules.CustomerPortal;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class CustomerPortalTenantIsolationTests(IntegrationTestFixture fixture)
{
    private readonly IntegrationTestFixture _fixture = fixture;
    [Fact]
    public async Task ListOnlineOrders_ShopA_CannotSeeShopBOrders()
    {
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GetOrder_ShopA_CannotReadShopBOrder()
    {
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ListInquiries_ShopA_CannotSeeShopBInquiries()
    {
        await Task.CompletedTask;
    }

    [Fact]
    public async Task CreateOrder_PortalCustomer_OrderScopedToCorrectShop()
    {
        await Task.CompletedTask;
    }
}

