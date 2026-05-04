using System.Net;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Masters;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class MastersSubscriptionGateTests(IntegrationTestFixture fixture)
{
    // Master data endpoints have no [RequireFeature] — available to all plans.

    [Fact]
    public async Task ListCountries_AllPlans_Returns200()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/masters/countries");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchHsnSac_AllPlans_Returns200()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/masters/hsn-sac?q=85");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListCurrencies_AllPlans_Returns200()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/masters/currencies");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDdl_NoFeatureClaim_Returns200()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/ddl/PAYMENT_MODE");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
