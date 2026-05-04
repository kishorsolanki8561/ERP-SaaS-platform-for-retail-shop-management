using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Modules.Wallet;

/// <summary>
/// Verifies that every wallet mutation produces a correct <c>AuditLog</c> row.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "AuditTrail")]
public sealed class WalletAuditTrailTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task WalletCredit_ProducesAuditLogRow()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var client = fixture.CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var createCustomerResp = await client.PostAsJsonAsync("/api/crm/customers", new
        {
            DisplayName  = $"Audit Wallet Customer {suffix}",
            CustomerType = "RETAIL",
            Email        = $"audit-wallet-{suffix}@test.local",
            Phone        = (string?)null,
            GstNumber    = (string?)null,
            CreditLimit  = 0m,
            GroupId      = (long?)null
        });
        createCustomerResp.IsSuccessStatusCode.Should().BeTrue("setup: customer creation must succeed");

        // BaseController.Ok<T>(Result<T>) returns OkObjectResult(result.Value) — raw long.
        var custJson   = await createCustomerResp.Content.ReadFromJsonAsync<JsonElement>();
        var customerId = custJson.GetInt64();

        var beforeUtc = DateTime.UtcNow.AddSeconds(-1);

        // ── Act: credit the wallet ────────────────────────────────────────────
        var creditResp = await client.PostAsJsonAsync("/api/wallet/credit", new
        {
            CustomerId    = customerId,
            CustomerName  = $"Audit Wallet Customer {suffix}",
            Amount        = 300m,
            ReferenceType = "Manual",
            ReferenceId   = (long?)null,
            Notes         = "Audit trail integration test"
        });
        creditResp.IsSuccessStatusCode.Should().BeTrue("wallet credit must succeed");

        // ── Assert: AuditLog row was written ──────────────────────────────────
        await using var scope = fixture.CreateScope();
        var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();

        // The AuditSaveChangesInterceptor writes rows for WalletBalance and/or
        // WalletTransaction whenever those entities are created or updated.
        var auditRows = await logDb.AuditLogs
            .Where(a =>
                (a.EntityName == "WalletBalance" || a.EntityName == "WalletTransaction")
                && a.OccurredAtUtc >= beforeUtc)
            .ToListAsync();

        auditRows.Should().NotBeEmpty(
            "AuditSaveChangesInterceptor must write at least one AuditLog row for a wallet credit");
    }
}
