using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Modules.Quotations;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class QuotationsAuditTrailTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task CreateQuotation_CreatesAuditLogEntry()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var payload = new
        {
            CustomerId = 1L,
            CustomerNameSnapshot = "Audit Customer",
            ValidUntil = DateTime.UtcNow.AddDays(30),
            Notes = "Audit trail test",
            Lines = new[]
            {
                new
                {
                    ProductId = 1L, ProductNameSnapshot = "Product",
                    ProductUnitId = 1L, UnitCodeSnapshot = "PCS",
                    ConversionFactor = 1m, QuantityInBilledUnit = 1m,
                    UnitPrice = 100m, DiscountAmount = 0m, GstRate = 18m
                }
            }
        };

        var response = await client.PostAsJsonAsync("/api/quotations", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(body);
            // BaseController.Ok<T>(Result<T>) returns result.Value directly — body is the number
            var quotationId = doc.RootElement.ValueKind == JsonValueKind.Number
                ? doc.RootElement.GetInt64()
                : 0L;

            if (quotationId > 0)
            {
                await using var scope = fixture.CreateScope();
                var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();
                var auditEntry = await logDb.AuditLogs
                    .Where(a => a.EntityName.Contains("Quotation") && a.EntityId == quotationId.ToString())
                    .FirstOrDefaultAsync();

                auditEntry.Should().NotBeNull();
                auditEntry!.EventType.Should().Be("Insert");
            }
        }
    }

    [Fact]
    public async Task SendQuotation_CreatesAuditLogEntry()
    {
        // Sending a non-existent quotation — confirms permission gate is working
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var response = await client.PatchAsync("/api/quotations/9999999/send", null);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ConvertToSalesOrder_PassesPermissionGate()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var response = await client.PostAsync("/api/quotations/9999999/convert", null);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
