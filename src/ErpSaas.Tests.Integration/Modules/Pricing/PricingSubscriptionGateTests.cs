using System.Net;
using System.Net.Http.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Pricing;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class PricingSubscriptionGateTests(IntegrationTestFixture fixture)
{
    // PricingController has no [RequireFeature] attributes — all endpoints are
    // permission-gated only. Subscription gate tests verify endpoints work
    // without feature claims.

    [Fact]
    public async Task ListDiscountRules_NoFeatureClaim_Returns200()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/pricing/discount-rules");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PricingCalculate_NoFeatureClaim_Returns200OrValidResponse()
    {
        // POST /api/pricing/calculate with an empty cart is valid
        var client = fixture.CreateNoFeatureClient();
        var payload = new
        {
            CustomerId = (long?)null,
            CustomerTypeId = (long?)null,
            Date = DateTime.UtcNow,
            Lines = Array.Empty<object>()
        };
        var response = await client.PostAsJsonAsync("/api/pricing/calculate", payload);
        // Should not be 401 or 403
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
