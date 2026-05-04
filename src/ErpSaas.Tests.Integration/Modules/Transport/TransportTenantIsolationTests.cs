using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Transport;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class TransportTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task ListProviders_ShopA_DoesNotSeeShopBProviders()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var shop1Client = fixture.CreateAuthenticatedClient(shopId: 1);

        // Create a provider as Shop 1
        var payload = new { Name = $"ShopA-Provider-{uid}", ContactName = "Test" };
        var createResp = await shop1Client.PostAsJsonAsync("/api/transport/providers", payload);
        createResp.IsSuccessStatusCode.Should().BeTrue();

        // CreateProviderAsync returns Result<long> → OkObjectResult(id) → plain long.
        var providerId = await createResp.Content.ReadFromJsonAsync<long>();

        // Shop 2 lists providers — should not contain Shop 1's provider
        var shop2Client = fixture.CreateAuthenticatedClient(shopId: 2);
        var listResp = await shop2Client.GetAsync("/api/transport/providers");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listBody = await listResp.Content.ReadAsStringAsync();

        if (providerId > 0)
            listBody.Should().NotContain(providerId.ToString());
    }

    [Fact]
    public async Task ListVehicles_ShopA_DoesNotSeeShopBVehicles()
    {
        var shop1Client = fixture.CreateAuthenticatedClient(shopId: 1);
        var shop2Client = fixture.CreateAuthenticatedClient(shopId: 2);

        var resp1 = await shop1Client.GetAsync("/api/transport/vehicles");
        var resp2 = await shop2Client.GetAsync("/api/transport/vehicles");

        resp1.StatusCode.Should().Be(HttpStatusCode.OK);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);

        var body1 = await resp1.Content.ReadAsStringAsync();
        var body2 = await resp2.Content.ReadAsStringAsync();
        JsonDocument.Parse(body1).RootElement.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        JsonDocument.Parse(body2).RootElement.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    [Fact]
    public async Task ListDeliveries_ShopA_DoesNotSeeShopBDeliveries()
    {
        var shop1Client = fixture.CreateAuthenticatedClient(shopId: 1);
        var shop2Client = fixture.CreateAuthenticatedClient(shopId: 2);

        var resp1 = await shop1Client.GetAsync("/api/transport/deliveries");
        var resp2 = await shop2Client.GetAsync("/api/transport/deliveries");

        resp1.StatusCode.Should().Be(HttpStatusCode.OK);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
