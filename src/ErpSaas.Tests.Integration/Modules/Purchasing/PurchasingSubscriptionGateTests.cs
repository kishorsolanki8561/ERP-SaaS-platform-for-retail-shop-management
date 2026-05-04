using System.Net;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Purchasing;

/// <summary>
/// Purchasing has no [RequireFeature] gating — core purchasing is available on all plans.
/// These tests verify that authenticated users with the right permission always get through
/// regardless of which feature flags are present.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "SubscriptionGate")]
public class PurchasingSubscriptionGateTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task PurchasingEndpoints_AllPlans_Returns200()
    {
        // Arrange: client is authenticated but has no feature flags (simulates Starter plan)
        // Purchasing has no [RequireFeature] so this must still succeed
        var client = fixture.CreateNoFeatureClient(shopId: 1);

        var response = await client.GetAsync("/api/purchasing/suppliers");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Purchasing.View is not feature-gated; it must be accessible on all plans");
    }
}
