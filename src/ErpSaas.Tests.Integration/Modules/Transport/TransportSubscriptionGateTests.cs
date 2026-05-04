using System.Net;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Transport;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class TransportSubscriptionGateTests(IntegrationTestFixture fixture)
{
    // TransportController has no [RequireFeature] attributes — all endpoints
    // are permission-gated only. These tests confirm endpoints work without
    // feature claims (no subscription gating).

    [Fact]
    public async Task ListProviders_NoFeatureClaim_Returns200()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/transport/providers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListVehicles_NoFeatureClaim_Returns200()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/transport/vehicles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListDeliveries_NoFeatureClaim_Returns200()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/transport/deliveries");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListProviders_WithFeatureClaim_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(features: ["Transport.VehicleTracking"]);
        var response = await client.GetAsync("/api/transport/providers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
