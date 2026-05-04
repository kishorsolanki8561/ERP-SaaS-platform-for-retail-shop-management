using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Modules.Accounting;

/// <summary>
/// Verifies that every mutating Accounting operation produces a correct AuditLog row.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "AuditTrail")]
public class AccountingAuditTrailTests(IntegrationTestFixture fixture)
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<long> GetFirstAccountGroupIdAsync(HttpClient client)
    {
        // Ensure the COA is seeded for shop 1 before querying.
        await fixture.SeedTenantDataAsync(1);
        var resp = await client.GetAsync("/api/accounting/account-groups");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var groups = await resp.Content.ReadFromJsonAsync<JsonElement[]>();
        groups.Should().NotBeNullOrEmpty();
        return groups![0].GetProperty("id").GetInt64();
    }

    private async Task<long> GetAccountByCodeAsync(HttpClient client, string code)
    {
        // Ensure the COA is seeded for shop 1 before querying.
        await fixture.SeedTenantDataAsync(1);
        var resp = await client.GetAsync("/api/accounting/accounts?pageSize=100");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await resp.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var item in doc.GetProperty("items").EnumerateArray())
        {
            if (item.GetProperty("code").GetString() == code)
                return item.GetProperty("id").GetInt64();
        }
        throw new InvalidOperationException($"Account with code '{code}' not found");
    }

    private async Task<long> CreateBalancedVoucherAsync(HttpClient client)
    {
        var cashId = await GetAccountByCodeAsync(client, "1010");
        var bankId = await GetAccountByCodeAsync(client, "1020");

        var resp = await client.PostAsJsonAsync("/api/accounting/vouchers", new
        {
            VoucherDate = DateTime.UtcNow,
            VoucherType = "Journal",
            Narration = "Audit trail test",
            SourceDocumentType = (string?)null,
            SourceDocumentId = (long?)null,
            Entries = new[]
            {
                new { AccountId = cashId, Type = "Debit",  Amount = 100m, Narration = (string?)null },
                new { AccountId = bankId, Type = "Credit", Amount = 100m, Narration = (string?)null }
            }
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        return await resp.Content.ReadFromJsonAsync<long>();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAccount_ProducesAuditLogRow()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var groupId = await GetFirstAccountGroupIdAsync(client);
        var code = $"AU{Guid.NewGuid().ToString("N")[..6]}";

        var before = DateTime.UtcNow.AddSeconds(-1);

        var resp = await client.PostAsJsonAsync("/api/accounting/accounts", new
        {
            Name = $"Audit Account {code}",
            Code = code,
            AccountGroupId = groupId,
            OpeningBalance = 0m,
            OpeningBalanceType = "Debit"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var accountId = await resp.Content.ReadFromJsonAsync<long>();

        // Check AuditLog in LogDbContext
        await using var scope = fixture.CreateScope();
        var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();

        var auditRow = await logDb.AuditLogs
            .Where(a => a.EntityName == "Account"
                     && a.EntityId == accountId.ToString()
                     && a.OccurredAtUtc >= before)
            .FirstOrDefaultAsync();

        auditRow.Should().NotBeNull("creating an Account must produce an AuditLog row");
    }

    [Fact]
    public async Task PostVoucher_ProducesAuditLogRow()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var voucherId = await CreateBalancedVoucherAsync(client);

        var before = DateTime.UtcNow.AddSeconds(-1);

        var postResp = await client.PostAsync(
            $"/api/accounting/vouchers/{voucherId}/post", null);
        postResp.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = fixture.CreateScope();
        var logDb = scope.ServiceProvider.GetRequiredService<LogDbContext>();

        var auditRow = await logDb.AuditLogs
            .Where(a => a.EntityName == "Voucher"
                     && a.EntityId == voucherId.ToString()
                     && a.OccurredAtUtc >= before)
            .FirstOrDefaultAsync();

        auditRow.Should().NotBeNull("posting a Voucher must produce an AuditLog row");
    }
}
