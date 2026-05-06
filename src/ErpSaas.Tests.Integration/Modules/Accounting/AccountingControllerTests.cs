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
/// Integration tests for AccountingController exercised through the full HTTP pipeline.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class AccountingControllerTests(IntegrationTestFixture fixture)
{
    // ── Helper: get first account group ID from seeded COA ────────────────────

    private async Task<long> GetFirstAccountGroupIdAsync()
    {
        // Ensure the COA is seeded for shop 1 before querying.
        await fixture.SeedTenantDataAsync(1);
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var resp = await client.GetAsync("/api/accounting/account-groups");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement[]>();
        body.Should().NotBeNullOrEmpty();
        return body![0].GetProperty("id").GetInt64();
    }

    private async Task<long> GetCashAccountIdAsync()
    {
        // Ensure the COA is seeded for shop 1 before querying.
        await fixture.SeedTenantDataAsync(1);
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var resp = await client.GetAsync("/api/accounting/accounts?pageSize=50");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var items = doc.GetProperty("items");
        // Cash account has code "1010"
        foreach (var item in items.EnumerateArray())
        {
            if (item.GetProperty("code").GetString() == "1010")
                return item.GetProperty("id").GetInt64();
        }
        throw new InvalidOperationException("Cash account (1010) not found in seeded COA");
    }

    // ── GET /api/accounting/account-groups ────────────────────────────────────

    [Fact]
    public async Task ListAccountGroups_Unauthenticated_Returns401()
    {
        var client = fixture.CreateUnauthenticatedClient();

        var response = await client.GetAsync("/api/accounting/account-groups");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListAccountGroups_WithPermission_Returns200AndList()
    {
        await fixture.SeedTenantDataAsync(1);
        var client = fixture.CreateAuthenticatedClient(shopId: 1);

        var response = await client.GetAsync("/api/accounting/account-groups");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        body.Should().NotBeNullOrEmpty("seeded COA includes account groups");
    }

    // ── GET /api/accounting/accounts ─────────────────────────────────────────

    [Fact]
    public async Task ListAccounts_WithoutPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(shopId: 1, permissionCode: "Other.View");

        var response = await client.GetAsync("/api/accounting/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListAccounts_WithPermission_Returns200AndPagedList()
    {
        await fixture.SeedTenantDataAsync(1);
        var client = fixture.CreateAuthenticatedClient(shopId: 1);

        var response = await client.GetAsync("/api/accounting/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
        doc.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0,
            "seeded COA has default accounts");
    }

    // ── POST /api/accounting/accounts ────────────────────────────────────────

    [Fact]
    public async Task CreateAccount_ValidRequest_Returns200WithId()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var groupId = await GetFirstAccountGroupIdAsync();
        var code = $"T{Guid.NewGuid().ToString("N")[..6]}";

        var response = await client.PostAsJsonAsync("/api/accounting/accounts", new
        {
            Name = $"Test Account {code}",
            Code = code,
            AccountGroupId = groupId,
            OpeningBalance = 0m,
            OpeningBalanceType = "Debit"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<long>();
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAccount_DuplicateCode_Returns409()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var groupId = await GetFirstAccountGroupIdAsync();
        var code = $"D{Guid.NewGuid().ToString("N")[..6]}";

        // First create succeeds
        await client.PostAsJsonAsync("/api/accounting/accounts", new
        {
            Name = $"Dupe A {code}",
            Code = code,
            AccountGroupId = groupId,
            OpeningBalance = 0m,
            OpeningBalanceType = "Debit"
        });

        // Second create with same code must conflict
        var response = await client.PostAsJsonAsync("/api/accounting/accounts", new
        {
            Name = $"Dupe B {code}",
            Code = code,
            AccountGroupId = groupId,
            OpeningBalance = 0m,
            OpeningBalanceType = "Debit"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateAccount_WithoutPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(shopId: 1, permissionCode: "Accounting.View");

        var response = await client.PostAsJsonAsync("/api/accounting/accounts", new
        {
            Name = "No Permission Account",
            Code = "NOPERM",
            AccountGroupId = 1,
            OpeningBalance = 0m,
            OpeningBalanceType = "Debit"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/accounting/vouchers ────────────────────────────────────────

    [Fact]
    public async Task CreateVoucher_BalancedEntries_Returns200WithId()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);

        // Use seeded accounts: Cash (1010) and Bank (1020)
        var cashId = await GetCashAccountIdAsync();
        var accountsResp = await client.GetAsync("/api/accounting/accounts?pageSize=50");
        var doc = await accountsResp.Content.ReadFromJsonAsync<JsonElement>();
        long bankId = 0;
        foreach (var item in doc.GetProperty("items").EnumerateArray())
        {
            if (item.GetProperty("code").GetString() == "1020")
            {
                bankId = item.GetProperty("id").GetInt64();
                break;
            }
        }
        bankId.Should().BeGreaterThan(0, "Bank account (1020) must exist in seeded COA");

        var response = await client.PostAsJsonAsync("/api/accounting/vouchers", new
        {
            VoucherDate = DateTime.UtcNow,
            VoucherType = "Journal",
            Narration = "Integration test journal",
            SourceDocumentType = (string?)null,
            SourceDocumentId = (long?)null,
            Entries = new[]
            {
                new { AccountId = cashId, Type = "Debit",  Amount = 1000m, Narration = (string?)null },
                new { AccountId = bankId, Type = "Credit", Amount = 1000m, Narration = (string?)null }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<long>();
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateVoucher_ImbalancedEntries_Returns409()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var cashId = await GetCashAccountIdAsync();

        var response = await client.PostAsJsonAsync("/api/accounting/vouchers", new
        {
            VoucherDate = DateTime.UtcNow,
            VoucherType = "Journal",
            Narration = "Imbalanced test",
            SourceDocumentType = (string?)null,
            SourceDocumentId = (long?)null,
            Entries = new[]
            {
                new { AccountId = cashId, Type = "Debit",  Amount = 500m,  Narration = (string?)null },
                new { AccountId = cashId, Type = "Credit", Amount = 300m, Narration = (string?)null }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateVoucher_WithoutPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(shopId: 1, permissionCode: "Accounting.View");

        var response = await client.PostAsJsonAsync("/api/accounting/vouchers", new
        {
            VoucherDate = DateTime.UtcNow,
            VoucherType = "Journal",
            Narration = "No perm",
            SourceDocumentType = (string?)null,
            SourceDocumentId = (long?)null,
            Entries = Array.Empty<object>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/accounting/vouchers/{id}/post ───────────────────────────────

    private async Task<long> CreateBalancedVoucherAsync(HttpClient client)
    {
        var cashId = await GetCashAccountIdAsync();
        var accountsResp = await client.GetAsync("/api/accounting/accounts?pageSize=50");
        var doc = await accountsResp.Content.ReadFromJsonAsync<JsonElement>();
        long bankId = 0;
        foreach (var item in doc.GetProperty("items").EnumerateArray())
        {
            if (item.GetProperty("code").GetString() == "1020")
            {
                bankId = item.GetProperty("id").GetInt64();
                break;
            }
        }

        var resp = await client.PostAsJsonAsync("/api/accounting/vouchers", new
        {
            VoucherDate = DateTime.UtcNow,
            VoucherType = "Journal",
            Narration = "Post test",
            SourceDocumentType = (string?)null,
            SourceDocumentId = (long?)null,
            Entries = new[]
            {
                new { AccountId = cashId, Type = "Debit",  Amount = 200m, Narration = (string?)null },
                new { AccountId = bankId, Type = "Credit", Amount = 200m, Narration = (string?)null }
            }
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        return await resp.Content.ReadFromJsonAsync<long>();
    }

    [Fact]
    public async Task PostVoucher_DraftVoucher_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var voucherId = await CreateBalancedVoucherAsync(client);

        var response = await client.PostAsync($"/api/accounting/vouchers/{voucherId}/post", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostVoucher_AlreadyPosted_Returns409()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var voucherId = await CreateBalancedVoucherAsync(client);

        // First post
        await client.PostAsync($"/api/accounting/vouchers/{voucherId}/post", null);

        // Second post — must conflict
        var response = await client.PostAsync($"/api/accounting/vouchers/{voucherId}/post", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PostVoucher_NotFound_Returns404()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);

        var response = await client.PostAsync("/api/accounting/vouchers/999999999/post", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/accounting/vouchers/{id}/reverse ────────────────────────────

    [Fact]
    public async Task ReverseVoucher_PostedVoucher_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var voucherId = await CreateBalancedVoucherAsync(client);

        // Must post before reversing
        await client.PostAsync($"/api/accounting/vouchers/{voucherId}/post", null);

        var response = await client.PostAsJsonAsync(
            $"/api/accounting/vouchers/{voucherId}/reverse",
            new { Narration = "Reversal integration test" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/accounting/expenses ────────────────────────────────────────

    [Fact]
    public async Task CreateExpense_ValidRequest_Returns200WithId()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1);
        var cashId = await GetCashAccountIdAsync();

        var response = await client.PostAsJsonAsync("/api/accounting/expenses", new
        {
            ExpenseDate = DateTime.UtcNow,
            AccountId = cashId,
            Description = "Integration test expense",
            Amount = 250m,
            PaymentModeCode = "CASH",
            PaidFromAccountId = (long?)null,
            IsRecurring = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<long>();
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateExpense_WithoutPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(shopId: 1, permissionCode: "Accounting.View");

        var response = await client.PostAsJsonAsync("/api/accounting/expenses", new
        {
            ExpenseDate = DateTime.UtcNow,
            AccountId = 1,
            Description = "No perm",
            Amount = 100m,
            PaymentModeCode = "CASH",
            PaidFromAccountId = (long?)null,
            IsRecurring = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/accounting/financial-years/{id}/close ──────────────────────

    [Fact]
    public async Task CloseFinancialYear_OpenYear_Returns200()
    {
        // Need feature "Accounting.Basic" in JWT
        var client = fixture.CreateAuthenticatedClient(
            shopId: 1,
            permissions: ["*"],
            features: ["Accounting.Basic"]);

        // Create a financial year first
        var createResp = await client.PostAsJsonAsync("/api/accounting/financial-years",
            new { StartYear = 2022 });
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var yearId = await createResp.Content.ReadFromJsonAsync<long>();
        yearId.Should().BeGreaterThan(0);

        // Close it
        var response = await client.PostAsync($"/api/accounting/financial-years/{yearId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CloseFinancialYear_FeatureOff_Returns402()
    {
        // No feature claims → feature gate should block
        var client = fixture.CreateNoFeatureClient(shopId: 1);

        // Create a financial year as admin (bypass feature gate for creation)
        var adminClient = fixture.CreateAuthenticatedClient(
            shopId: 1,
            permissions: ["*"],
            features: ["Accounting.Basic"]);
        var createResp = await adminClient.PostAsJsonAsync("/api/accounting/financial-years",
            new { StartYear = 2021 });
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var yearId = await createResp.Content.ReadFromJsonAsync<long>();

        // Now try to close without the feature flag
        var response = await client.PostAsync($"/api/accounting/financial-years/{yearId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
    }
}
