using System.Net;
using System.Net.Http.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.ApiAccess;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class ApiAccessSubscriptionGateTests(IntegrationTestFixture fixture)
{
    [Fact(Skip = "Testcontainers gate pending")]
    public async Task CreateApiKey_StarterPlan_Returns402()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/shop-api-keys",
            new { name = "Key", scopesCsv = (string?)null, expiresAtUtc = (DateTime?)null });
        response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
    }

    [Fact(Skip = "Testcontainers gate pending")]
    public async Task RegisterEndpoint_StarterPlan_Returns402()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/webhooks/endpoints",
            new { name = "Test", url = "https://example.com/hook", eventsCsv = "invoice.finalized" });
        response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
    }
}
