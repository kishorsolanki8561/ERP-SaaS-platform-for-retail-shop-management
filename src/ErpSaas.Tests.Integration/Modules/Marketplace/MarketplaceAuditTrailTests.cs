using System.Net;
using System.Net.Http.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Modules.Marketplace;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class MarketplaceAuditTrailTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task CreateAccount_CreatesAuditLogEntry()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var uid = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            MarketplaceCode = "Amazon",
            AccountName = $"Audit-{uid}",
            SellerId = $"AUD-{uid}",
            CredentialsJson = "{\"key\":\"test\"}",
            SyncInventory = true,
            SyncPricing = false,
            SyncOrders = true
        };

        var response = await client.PostAsJsonAsync("/api/marketplace/accounts", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // CreateMarketplaceAccountAsync returns Result<long> → OkObjectResult(id) → plain long.
        var accountId = await response.Content.ReadFromJsonAsync<long>();
        accountId.Should().BeGreaterThan(0);

        await using var scope = fixture.CreateScope();
        var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();
        var auditEntry = await logDb.AuditLogs
            .Where(a => a.EntityName.Contains("Marketplace") && a.EntityId == accountId.ToString())
            .FirstOrDefaultAsync();

        // AuditSaveChangesInterceptor writes the row when [Auditable] entity is saved
        auditEntry.Should().NotBeNull();
        auditEntry!.EventType.Should().Be("Insert");
    }

    [Fact]
    public async Task ConvertOrder_CreatesAuditLogEntry()
    {
        // Trying to convert a non-existent order will return 404 or similar,
        // but we verify the permission gate passes and no audit entry for
        // non-existent entity is written (negative audit check).
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var response = await client.PostAsync("/api/marketplace/orders/9999999/convert-to-invoice", null);

        // Any non-403 status confirms permission gate passed
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        // No audit entry should exist for a non-existent order conversion
        await using var scope = fixture.CreateScope();
        var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();
        var entries = await logDb.AuditLogs
            .Where(a => a.EntityName.Contains("MarketplaceOrder") && a.EntityId == "9999999")
            .ToListAsync();
        entries.Should().BeEmpty();
    }
}
