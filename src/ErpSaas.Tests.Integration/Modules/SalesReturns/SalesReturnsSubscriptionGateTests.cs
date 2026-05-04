using System.Net;
using System.Net.Http.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.SalesReturns;

/// <summary>
/// SalesReturns endpoints carry no [RequireFeature] gate — they are available
/// on all subscription plans.  These tests verify that unauthenticated callers
/// still receive 401, so the auth layer is always enforced.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "SubscriptionGate")]
public sealed class SalesReturnsSubscriptionGateTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task SalesReturnsEndpoints_AllPlans_Returns401WithNoAuth()
    {
        // SalesReturns has no [RequireFeature] — all plans may use it.
        // The authentication layer must still reject unauthenticated requests.
        var client = fixture.CreateUnauthenticatedClient();

        var postResponse    = await client.PostAsJsonAsync("/api/sales-returns", new { });
        var approveResponse = await client.PostAsync("/api/sales-returns/1/approve", null);
        var cancelResponse  = await client.PostAsync("/api/sales-returns/1/cancel", null);
        var cnResponse      = await client.PostAsJsonAsync("/api/sales-returns/credit-notes", new { });

        postResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        cnResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
