using System.Net;
using System.Net.Http.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.ApiAccess;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class ApiAccessControllerTests(IntegrationTestFixture fixture)
{
    // ── GET /api/shop-api-keys ────────────────────────────────────────────────

    [Fact(Skip = "Testcontainers gate pending")]
    public async Task ListApiKeys_WithoutAuth_Returns401()
    {
        var client = fixture.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/shop-api-keys");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(Skip = "Testcontainers gate pending")]
    public async Task ListApiKeys_Authenticated_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/shop-api-keys");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/shop-api-keys ───────────────────────────────────────────────

    [Fact(Skip = "Testcontainers gate pending")]
    public async Task CreateApiKey_ValidBody_Returns200WithRawKey()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/shop-api-keys",
            new { name = "Test Key", scopesCsv = (string?)null, expiresAtUtc = (DateTime?)null });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/webhooks/endpoints ─────────────────────────────────────────

    [Fact(Skip = "Testcontainers gate pending")]
    public async Task RegisterEndpoint_WithoutAuth_Returns401()
    {
        var client = fixture.CreateUnauthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/webhooks/endpoints",
            new { name = "Test", url = "https://example.com/hook", eventsCsv = "invoice.finalized" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(Skip = "Testcontainers gate pending")]
    public async Task RegisterEndpoint_HttpUrl_Returns400()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/webhooks/endpoints",
            new { name = "Test", url = "http://example.com/hook", eventsCsv = "invoice.finalized" });
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    // ── GET /api/webhooks/events ──────────────────────────────────────────────

    [Fact(Skip = "Testcontainers gate pending")]
    public async Task GetEventCatalog_Authenticated_ReturnsList()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/webhooks/events");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
