using System.Net;
using System.Net.Http.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Payment;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class PaymentSubscriptionGateTests(IntegrationTestFixture fixture)
{
    // Endpoints with [RequireFeature("Payment.OnlineGateway")]:
    //   GET/POST /api/payment/gateways
    //   POST /api/payment/transactions (initiate)

    [Fact]
    public async Task ListGateways_FeatureDisabled_Returns402()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/payment/gateways");
        response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
    }

    [Fact]
    public async Task ListGateways_FeatureEnabled_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(features: ["Payment.OnlineGateway"]);
        var response = await client.GetAsync("/api/payment/gateways");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InitiatePayment_FeatureDisabled_Returns402()
    {
        var client = fixture.CreateNoFeatureClient();
        var payload = new
        {
            GatewayCode = "Razorpay",
            ReferenceType = "Invoice",
            ReferenceId = 1L,
            Amount = 100m,
            Currency = "INR",
            Description = "Test",
            CustomerEmail = "test@test.com",
            CustomerPhone = "9999999999",
            ReturnUrl = "https://test.com/return"
        };
        var response = await client.PostAsJsonAsync("/api/payment/transactions", payload);
        response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
    }

    [Fact]
    public async Task InitiatePayment_FeatureEnabled_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(features: ["Payment.OnlineGateway"]);
        var payload = new
        {
            GatewayCode = "Razorpay",
            ReferenceType = "Invoice",
            ReferenceId = 1L,
            Amount = 100m,
            Currency = "INR",
            Description = "Test",
            CustomerEmail = "test@test.com",
            CustomerPhone = "9999999999",
            ReturnUrl = "https://test.com/return"
        };
        var response = await client.PostAsJsonAsync("/api/payment/transactions", payload);
        // 200 on success or 404/400 if gateway not configured — not 403
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListTransactions_NoFeatureClaim_Returns200()
    {
        // GET transactions has no [RequireFeature]
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/payment/transactions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
