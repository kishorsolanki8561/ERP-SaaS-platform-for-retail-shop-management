using Dapper;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Seeds;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Infrastructure;

/// <summary>
/// Verifies that running <c>DatabaseSeeder.SeedAllAsync</c> twice produces
/// identical row counts — no duplicate inserts, no errors on second run.
/// Phase exit-gate: P1-8.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "Seeder")]
public sealed class SeederIdempotencyTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task SeedAllAsync_CalledTwice_RowCountsIdentical()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        await using var scope1 = fixture.CreateScope();
        var seeder = scope1.ServiceProvider.GetRequiredService<DatabaseSeeder>();

        // First run already happened during fixture initialization.
        // Record row counts for key seeded tables.
        var countsAfterFirstRun = await GetKeyRowCountsAsync(scope1.ServiceProvider);

        // ── Act ───────────────────────────────────────────────────────────────
        // Run the seeder a second time
        await seeder.SeedAllAsync();

        // ── Assert ────────────────────────────────────────────────────────────
        var countsAfterSecondRun = await GetKeyRowCountsAsync(scope1.ServiceProvider);

        foreach (var (table, count) in countsAfterFirstRun)
        {
            countsAfterSecondRun.Should().ContainKey(table);
            countsAfterSecondRun[table].Should()
                .Be(count, $"seeder must be idempotent — {table} row count changed on second run");
        }
    }

    [Fact]
    public async Task SeedAllAsync_NotificationTemplatesPresent_AfterFirstRun()
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpSaas.Infrastructure.Data.NotificationsDbContext>();

        var templates = await db.Set<ErpSaas.Infrastructure.Data.Entities.Messaging.NotificationTemplate>()
            .Select(t => t.Code)
            .ToListAsync();

        templates.Should().Contain("INVOICE_FINALIZED",
            "BillingSystemSeeder must create the INVOICE_FINALIZED notification template");
        templates.Should().Contain("WALLET_CREDITED",
            "WalletSystemSeeder must create the WALLET_CREDITED notification template");
        templates.Should().Contain("SHIFT_CLOSED",
            "ShiftSystemSeeder must create the SHIFT_CLOSED notification template");
    }

    private static async Task<Dictionary<string, int>> GetKeyRowCountsAsync(IServiceProvider sp)
    {
        var platformDb = sp.GetRequiredService<PlatformDbContext>();
        var notifDb    = sp.GetRequiredService<NotificationsDbContext>();

        // Open fresh connections from the connection string rather than reusing the
        // EF context's internal connection object, which may be in an unexpected state.
        await using var platformConn = new Microsoft.Data.SqlClient.SqlConnection(
            platformDb.Database.GetConnectionString());
        await using var notifConn = new Microsoft.Data.SqlClient.SqlConnection(
            notifDb.Database.GetConnectionString());

        await platformConn.OpenAsync();
        await notifConn.OpenAsync();

        return new Dictionary<string, int>
        {
            ["DdlCatalog"] = await platformConn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM [masters].[DdlCatalog]"),
            ["SequenceDefinition"] = await platformConn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM [masters].[SequenceDefinition]"),
            ["NotificationTemplate"] = await notifConn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM [messaging].[NotificationTemplate]"),
        };
    }
}
