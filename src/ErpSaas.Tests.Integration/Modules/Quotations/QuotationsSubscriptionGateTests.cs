using System.Net;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Quotations;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class QuotationsSubscriptionGateTests(IntegrationTestFixture fixture)
{
    // QuotationsController has no [RequireFeature] on its list/create endpoints.
    // Subscription gate tests verify endpoints work without feature claims.

    [Fact]
    public async Task ListQuotations_NoFeatureClaim_Returns200()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/quotations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListSalesOrders_NoFeatureClaim_Returns200()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/quotations/sales-orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListDeliveryChallans_NoFeatureClaim_Returns200()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/quotations/delivery-challans");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListQuotations_WithWholesaleQuotationsFeature_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(features: ["wholesale.quotations"]);
        var response = await client.GetAsync("/api/quotations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
