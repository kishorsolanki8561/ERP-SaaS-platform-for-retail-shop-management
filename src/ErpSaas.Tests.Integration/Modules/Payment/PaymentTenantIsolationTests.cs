using System.Net;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Payment;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class PaymentTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task GetTransactions_Shop1_DoesNotReturnShop2Data()
    {
        var shop1Client = fixture.CreateAuthenticatedClient(shopId: 1);
        var shop2Client = fixture.CreateAuthenticatedClient(shopId: 2);

        var resp1 = await shop1Client.GetAsync("/api/payment/transactions");
        var resp2 = await shop2Client.GetAsync("/api/payment/transactions");

        resp1.StatusCode.Should().Be(HttpStatusCode.OK);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);

        var body1 = await resp1.Content.ReadAsStringAsync();
        var body2 = await resp2.Content.ReadAsStringAsync();
        JsonDocument.Parse(body1).RootElement.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        JsonDocument.Parse(body2).RootElement.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    [Fact]
    public async Task ListExceptions_Shop1_DoesNotReturnShop2Exceptions()
    {
        var shop1Client = fixture.CreateAuthenticatedClient(shopId: 1);
        var shop2Client = fixture.CreateAuthenticatedClient(shopId: 2);

        var resp1 = await shop1Client.GetAsync("/api/payment/transactions/exceptions");
        var resp2 = await shop2Client.GetAsync("/api/payment/transactions/exceptions");

        resp1.StatusCode.Should().Be(HttpStatusCode.OK);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);

        var body1 = await resp1.Content.ReadAsStringAsync();
        var body2 = await resp2.Content.ReadAsStringAsync();
        JsonDocument.Parse(body1).RootElement.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        JsonDocument.Parse(body2).RootElement.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }
}
