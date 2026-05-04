using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Modules.SalesReturns;

/// <summary>
/// Verifies that SalesReturns mutations produce <c>AuditLog</c> rows in LogDb.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "AuditTrail")]
public sealed class SalesReturnsAuditTrailTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task IssueCreditNote_ProducesAuditLogRowForCreditNote()
    {
        // ── Arrange: create a CRM customer ────────────────────────────────────
        var client = fixture.CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var customerResp = await client.PostAsJsonAsync("/api/crm/customers", new
        {
            DisplayName  = $"Audit CN Customer {suffix}",
            CustomerType = "RETAIL",
            Email        = $"audit-cn-{suffix}@test.local",
            Phone        = (string?)null,
            GstNumber    = (string?)null,
            CreditLimit  = 0m,
            GroupId      = (long?)null
        });
        customerResp.IsSuccessStatusCode.Should().BeTrue("setup: customer creation must succeed");
        // BaseController.Ok<T>(Result<T>) returns OkObjectResult(result.Value) — raw long.
        var custJson   = await customerResp.Content.ReadFromJsonAsync<JsonElement>();
        var customerId = custJson.GetInt64();

        // Record the timestamp just before the action so we query only new rows.
        var beforeUtc = DateTime.UtcNow.AddSeconds(-1);

        // ── Act: issue a credit note ──────────────────────────────────────────
        var cnResp = await client.PostAsJsonAsync("/api/sales-returns/credit-notes", new
        {
            CustomerId = customerId,
            Amount     = 150m,
            Notes      = "Audit trail test",
            ExpiryDate = (DateTime?)null
        });
        cnResp.IsSuccessStatusCode.Should().BeTrue("IssueCreditNote must succeed for audit test");

        // ── Assert: AuditLog row for CreditNote was written ───────────────────
        await using var scope  = fixture.CreateScope();
        var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();

        var auditRow = await logDb.AuditLogs
            .Where(a => a.EntityName == "CreditNote" && a.OccurredAtUtc >= beforeUtc)
            .FirstOrDefaultAsync();

        auditRow.Should().NotBeNull(
            "AuditSaveChangesInterceptor must write an AuditLog row when a CreditNote is created");
        auditRow!.EventType.Should().NotBeNullOrWhiteSpace();
    }
}
