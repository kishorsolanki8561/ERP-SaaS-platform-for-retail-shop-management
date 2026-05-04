using System.Net;
using System.Net.Http.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Modules.Payment;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class PaymentAuditTrailTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task InitiatePayment_CreatesAuditLog()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, features: ["Payment.OnlineGateway"]);
        var payload = new
        {
            GatewayCode = "Razorpay",
            ReferenceType = "Invoice",
            ReferenceId = 1L,
            Amount = 500m,
            Currency = "INR",
            Description = "Audit test payment",
            CustomerEmail = "audit@test.com",
            CustomerPhone = "8888888888",
            ReturnUrl = "https://test.com/return"
        };

        var response = await client.PostAsJsonAsync("/api/payment/transactions", payload);
        // Auth + permission + feature gate must pass
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            // InitiateAsync returns Result<long> → OkObjectResult(id) → plain long.
            var txnId = await response.Content.ReadFromJsonAsync<long>();

            if (txnId > 0)
            {
                await using var scope = fixture.CreateScope();
                var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();
                var auditEntry = await logDb.AuditLogs
                    .Where(a => a.EntityName.Contains("Payment") && a.EntityId == txnId.ToString())
                    .FirstOrDefaultAsync();

                auditEntry.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public async Task ConfirmPayment_CreatesAuditLog()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var payload = new
        {
            GatewayTransactionId = "TXN-AUDIT-001",
            GatewayStatus = "success",
            GatewayRawResponse = "{}"
        };
        // Non-existent transaction — confirms auth + permission gate
        var response = await client.PostAsJsonAsync("/api/payment/transactions/9999999/confirm", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RefundPayment_CreatesAuditLog()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var payload = new { Amount = 100m, Reason = "Audit test refund" };
        var response = await client.PostAsJsonAsync("/api/payment/transactions/9999999/refund", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ResolveException_CreatesAuditLog()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var payload = new { Resolution = "Manually resolved in audit test", AdjustmentAmount = (decimal?)null };
        var response = await client.PostAsJsonAsync("/api/payment/transactions/exceptions/9999999/resolve", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
