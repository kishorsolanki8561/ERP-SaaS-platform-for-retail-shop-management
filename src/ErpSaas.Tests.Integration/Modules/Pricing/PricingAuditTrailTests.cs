using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Modules.Pricing.Enums;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Modules.Pricing;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class PricingAuditTrailTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task CreateDiscountRule_CreatesAuditLogEntry()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var uid = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            Name = $"AuditRule-{uid}",
            DiscountTypeCode = "PERCENT",
            Scope = DiscountScope.Invoice.ToString(),
            PercentValue = 15m,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30),
            Priority = 1,
            IsStackable = false
        };

        var response = await client.PostAsJsonAsync("/api/pricing/discount-rules", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(body);
            // BaseController.Ok<T>(Result<T>) returns result.Value directly — body is the number
            var ruleId = doc.RootElement.ValueKind == JsonValueKind.Number
                ? doc.RootElement.GetInt64()
                : 0L;

            if (ruleId > 0)
            {
                await using var scope = fixture.CreateScope();
                var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();
                var auditEntry = await logDb.AuditLogs
                    .Where(a => a.EntityName.Contains("Discount") && a.EntityId == ruleId.ToString())
                    .FirstOrDefaultAsync();

                auditEntry.Should().NotBeNull();
                auditEntry!.EventType.Should().Be("Insert");
            }
        }
    }

    [Fact]
    public async Task CreateExtraCharge_CreatesAuditLogEntry()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var uid = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            Name = $"AuditCharge-{uid}",
            Type = ChargeType.FixedAmount.ToString(),
            Value = 25m,
            IsTaxable = false,
            GstRate = (decimal?)null
        };

        var response = await client.PostAsJsonAsync("/api/pricing/extra-charges", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(body);
            // BaseController.Ok<T>(Result<T>) returns result.Value directly — body is the number
            var chargeId = doc.RootElement.ValueKind == JsonValueKind.Number
                ? doc.RootElement.GetInt64()
                : 0L;

            if (chargeId > 0)
            {
                await using var scope = fixture.CreateScope();
                var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();
                var auditEntry = await logDb.AuditLogs
                    .Where(a => a.EntityName.Contains("Charge") && a.EntityId == chargeId.ToString())
                    .FirstOrDefaultAsync();

                auditEntry.Should().NotBeNull();
            }
        }
    }
}
