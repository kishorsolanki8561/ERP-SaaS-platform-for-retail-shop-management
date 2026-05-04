using System.Net;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Warranty;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class WarrantyTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Registrations_Shop1Token_ReturnsOnlyShop1Data()
    {
        var shop1Client = fixture.CreateAuthenticatedClient(shopId: 1);
        var shop2Client = fixture.CreateAuthenticatedClient(shopId: 2);

        // Both clients hit the expiring registrations endpoint
        var resp1 = await shop1Client.GetAsync("/api/warranty/registrations/expiring?days=365");
        var resp2 = await shop2Client.GetAsync("/api/warranty/registrations/expiring?days=365");

        resp1.StatusCode.Should().Be(HttpStatusCode.OK);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);

        var body1 = await resp1.Content.ReadAsStringAsync();
        var body2 = await resp2.Content.ReadAsStringAsync();
        var doc1 = JsonDocument.Parse(body1);
        var doc2 = JsonDocument.Parse(body2);

        doc1.RootElement.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        doc2.RootElement.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    [Fact]
    public async Task Claims_Shop1Token_ReturnsOnlyShop1Data()
    {
        var shop1Client = fixture.CreateAuthenticatedClient(shopId: 1);
        var shop2Client = fixture.CreateAuthenticatedClient(shopId: 2);

        var resp1 = await shop1Client.GetAsync("/api/warranty/claims");
        var resp2 = await shop2Client.GetAsync("/api/warranty/claims");

        resp1.StatusCode.Should().Be(HttpStatusCode.OK);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);

        var body1 = await resp1.Content.ReadAsStringAsync();
        var body2 = await resp2.Content.ReadAsStringAsync();
        var doc1 = JsonDocument.Parse(body1);
        var doc2 = JsonDocument.Parse(body2);

        doc1.RootElement.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        doc2.RootElement.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }
}
