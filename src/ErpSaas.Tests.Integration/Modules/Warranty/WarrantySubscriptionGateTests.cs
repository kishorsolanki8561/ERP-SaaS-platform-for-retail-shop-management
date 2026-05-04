using System.Net;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Warranty;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class WarrantySubscriptionGateTests(IntegrationTestFixture fixture)
{
    // WarrantyController has no [RequireFeature] attributes — endpoints are
    // permission-gated only. The subscription gate tests verify that the
    // endpoints are accessible regardless of feature claims.

    [Fact]
    public async Task Warranty_FeatureOff_Returns200()
    {
        // No feature gate on warranty endpoints — should return 200 with no feature claims
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/warranty/registrations/expiring?days=30");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Warranty_FeatureOn_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(features: ["Warranty.Management"]);
        var response = await client.GetAsync("/api/warranty/registrations/expiring?days=30");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListClaims_NoFeatureClaim_Returns200()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/warranty/claims");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
