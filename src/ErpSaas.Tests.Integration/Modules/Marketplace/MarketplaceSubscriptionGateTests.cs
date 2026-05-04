using System.Net;
using System.Net.Http.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Marketplace;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class MarketplaceSubscriptionGateTests(IntegrationTestFixture fixture)
{
    // Marketplace controller has no [RequireFeature] on account create/list endpoints,
    // so feature-gate tests verify the endpoints work with and without features.

    [Fact]
    public async Task ListAccounts_NoFeatureClaim_Returns200()
    {
        // Marketplace.View endpoints do not have [RequireFeature] — should work without features
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/marketplace/accounts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListOrders_NoFeatureClaim_Returns200()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/marketplace/orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateAccount_FeatureDisabled_Returns200()
    {
        // CreateAccount has no [RequireFeature] gate — returns 200 regardless of features
        var client = fixture.CreateNoFeatureClient();
        var uid = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            MarketplaceCode = "Flipkart",
            AccountName = $"NoFeat-{uid}",
            SellerId = $"FK-{uid}",
            CredentialsJson = "{\"key\":\"test\"}",
            SyncInventory = false,
            SyncPricing = false,
            SyncOrders = false
        };
        var response = await client.PostAsJsonAsync("/api/marketplace/accounts", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateAccount_FeatureEnabled_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(features: ["Marketplace.Amazon"]);
        var uid = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            MarketplaceCode = "Amazon",
            AccountName = $"Feat-{uid}",
            SellerId = $"AMZ-{uid}",
            CredentialsJson = "{\"key\":\"test\"}",
            SyncInventory = true,
            SyncPricing = false,
            SyncOrders = true
        };
        var response = await client.PostAsJsonAsync("/api/marketplace/accounts", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
