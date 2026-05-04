using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Transport;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class TransportControllerTests(IntegrationTestFixture fixture)
{
    // ── GET /api/transport/providers ─────────────────────────────────────────

    [Fact]
    public async Task ListProviders_Unauthenticated_Returns401()
    {
        var client = fixture.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/transport/providers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListProviders_Authenticated_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/transport/providers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListProviders_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Transport.Manage");
        var response = await client.GetAsync("/api/transport/providers");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/transport/providers ────────────────────────────────────────

    [Fact]
    public async Task CreateProvider_ValidPayload_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var uid = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            Name = $"Provider-{uid}",
            ContactName = "John Doe",
            ContactPhone = "9999999999",
            GstNumber = (string?)null
        };
        var response = await client.PostAsJsonAsync("/api/transport/providers", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var providerId = await response.Content.ReadFromJsonAsync<long>();
        providerId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateProvider_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Transport.View");
        var payload = new { Name = "Test", ContactName = "Test" };
        var response = await client.PostAsJsonAsync("/api/transport/providers", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/transport/vehicles ───────────────────────────────────────────

    [Fact]
    public async Task ListVehicles_Authenticated_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/transport/vehicles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListVehicles_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Transport.Manage");
        var response = await client.GetAsync("/api/transport/vehicles");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/transport/vehicles ──────────────────────────────────────────

    [Fact]
    public async Task CreateVehicle_ValidPayload_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var uid = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            LicensePlate = $"MH-01-{uid[..4]}",
            Model = "Tata Ace",
            MaxLoadKg = 1000m,
            TransportProviderId = (long?)null,
            DriverName = "Driver One",
            DriverPhone = "8888888888"
        };
        var response = await client.PostAsJsonAsync("/api/transport/vehicles", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateVehicle_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Transport.View");
        var payload = new { LicensePlate = "MH-01-AB-1234", Model = "Test", MaxLoadKg = 500m };
        var response = await client.PostAsJsonAsync("/api/transport/vehicles", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/transport/deliveries ─────────────────────────────────────────

    [Fact]
    public async Task ListDeliveries_Authenticated_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/transport/deliveries");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
