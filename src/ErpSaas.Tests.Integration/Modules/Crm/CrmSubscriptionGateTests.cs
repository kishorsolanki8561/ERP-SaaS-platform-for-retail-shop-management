using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Xunit;

namespace ErpSaas.Tests.Integration.Modules.Crm;

/// <summary>
/// CRM has no [RequireFeature] gates — all endpoints are available on every plan.
/// Verifies that list endpoints return 200 regardless of the features claim.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Module", "Crm")]
public class CrmSubscriptionGateTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task CrmEndpoints_AllPlans_Returns200()
    {
        // Arrange: client authenticated with all permissions but no feature flags
        // (simulates a shop on the lowest plan with no feature gates triggered)
        var client = fixture.CreateNoFeatureClient(shopId: 1);

        // Act: GET /api/crm/customers — no [RequireFeature] so must always be 200
        var response = await client.GetAsync("/api/crm/customers");

        // Assert: CRM list is always accessible regardless of feats claim
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task CrmGroupsEndpoint_AllPlans_Returns200()
    {
        var client = fixture.CreateNoFeatureClient(shopId: 1);

        var response = await client.GetAsync("/api/crm/groups");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }
}
