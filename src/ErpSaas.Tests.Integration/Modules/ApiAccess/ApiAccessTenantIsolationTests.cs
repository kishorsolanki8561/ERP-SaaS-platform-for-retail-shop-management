using ErpSaas.Tests.Integration.Fixtures;

namespace ErpSaas.Tests.Integration.Modules.ApiAccess;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class ApiAccessTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact(Skip = "Testcontainers gate pending")]
    public async Task ListApiKeys_ShopA_CannotSeeShopBKeys()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Testcontainers gate pending")]
    public async Task ListWebhookEndpoints_ShopA_CannotSeeShopBEndpoints()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Testcontainers gate pending")]
    public async Task RevokeApiKey_ShopA_CannotRevokeShopBKey()
    {
        await Task.CompletedTask;
    }
}
