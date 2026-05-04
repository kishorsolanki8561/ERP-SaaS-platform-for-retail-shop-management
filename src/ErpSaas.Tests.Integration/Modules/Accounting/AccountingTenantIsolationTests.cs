using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Accounting;

/// <summary>
/// Verifies that Accounting data from Shop A is never visible to Shop B.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "TenantIsolation")]
public class AccountingTenantIsolationTests(IntegrationTestFixture fixture)
{
    private const long ShopA = 1L;
    private const long ShopB = 2L;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<long> GetFirstAccountGroupIdForShopAsync(long shopId)
    {
        // Ensure the COA is seeded for the target shop before querying.
        await fixture.SeedTenantDataAsync(shopId);
        var client = fixture.CreateAuthenticatedClient(shopId: shopId);
        var resp = await client.GetAsync("/api/accounting/account-groups");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var groups = await resp.Content.ReadFromJsonAsync<JsonElement[]>();
        groups.Should().NotBeNullOrEmpty();
        return groups![0].GetProperty("id").GetInt64();
    }

    private async Task<long> CreateAccountAsync(long shopId, string code)
    {
        var client = fixture.CreateAuthenticatedClient(shopId: shopId);
        var groupId = await GetFirstAccountGroupIdForShopAsync(shopId);

        var resp = await client.PostAsJsonAsync("/api/accounting/accounts", new
        {
            Name = $"Isolation Test {code}",
            Code = code,
            AccountGroupId = groupId,
            OpeningBalance = 0m,
            OpeningBalanceType = "Debit"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        return await resp.Content.ReadFromJsonAsync<long>();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAccounts_ShopA_DoesNotReturnShopBAccounts()
    {
        // Arrange: create a custom account in Shop B
        var uniqueCode = $"ISO{Guid.NewGuid().ToString("N")[..5]}";
        await CreateAccountAsync(ShopB, uniqueCode);

        // Act: list accounts as Shop A
        var clientA = fixture.CreateAuthenticatedClient(shopId: ShopA);
        var response = await clientA.GetAsync("/api/accounting/accounts?pageSize=200");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
        var codes = doc.GetProperty("items").EnumerateArray()
            .Select(x => x.GetProperty("code").GetString())
            .ToList();

        codes.Should().NotContain(uniqueCode,
            "accounts created in Shop B must not be visible to Shop A");
    }

    [Fact]
    public async Task GetAccount_ShopBCannotReadShopAAccount_Returns404()
    {
        // Arrange: create an account in Shop A
        var uniqueCode = $"ISO{Guid.NewGuid().ToString("N")[..5]}";
        var accountId = await CreateAccountAsync(ShopA, uniqueCode);

        // Act: attempt to read it as Shop B
        var clientB = fixture.CreateAuthenticatedClient(shopId: ShopB);
        var response = await clientB.GetAsync($"/api/accounting/accounts/{accountId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Shop B must not be able to read Shop A's account");
    }
}
