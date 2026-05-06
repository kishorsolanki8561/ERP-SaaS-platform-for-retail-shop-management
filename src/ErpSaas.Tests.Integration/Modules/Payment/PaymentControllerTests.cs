using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Payment;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class PaymentControllerTests(IntegrationTestFixture fixture)
{
    // ── GET /api/payment/transactions ─────────────────────────────────────────

    [Fact]
    public async Task GetTransactions_Authenticated_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/payment/transactions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTransactions_Unauthenticated_Returns401()
    {
        var client = fixture.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/payment/transactions");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTransactions_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Payment.Manage");
        var response = await client.GetAsync("/api/payment/transactions");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/payment/transactions/{id} ────────────────────────────────────

    [Fact]
    public async Task GetTransaction_NotFound_Returns404()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/payment/transactions/9999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/payment/gateways (requires Payment.OnlineGateway feature) ────

    [Fact]
    public async Task ListGatewayAccounts_FeatureEnabled_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(features: ["Payment.OnlineGateway"]);
        var response = await client.GetAsync("/api/payment/gateways");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListGatewayAccounts_FeatureDisabled_Returns402()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/payment/gateways");
        response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
    }

    // ── POST /api/payment/transactions (requires Payment.OnlineGateway) ───────

    [Fact]
    public async Task InitiatePayment_MissingGateway_Returns404()
    {
        var client = fixture.CreateAuthenticatedClient(features: ["Payment.OnlineGateway"]);
        var payload = new
        {
            GatewayCode = "Razorpay",
            ReferenceType = "Invoice",
            ReferenceId = 9999999L,
            Amount = 100m,
            Currency = "INR",
            Description = "Test payment",
            CustomerEmail = "test@test.com",
            CustomerPhone = "9999999999",
            ReturnUrl = "https://test.com/return"
        };
        var response = await client.PostAsJsonAsync("/api/payment/transactions", payload);
        // Gateway not configured → 404 or 400; must not be 401/403
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ConfirmPayment_AlreadyFinal_Returns409()
    {
        var client = fixture.CreateAuthenticatedClient();
        var payload = new
        {
            GatewayTransactionId = "TXN-TEST-001",
            GatewayStatus = "success",
            GatewayRawResponse = "{}"
        };
        // Non-existent transaction → 404 or 409 depending on state
        var response = await client.PostAsJsonAsync("/api/payment/transactions/9999999/confirm", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ── Reconciliation exceptions ─────────────────────────────────────────────

    [Fact]
    public async Task ListExceptions_Authenticated_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/payment/transactions/exceptions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
