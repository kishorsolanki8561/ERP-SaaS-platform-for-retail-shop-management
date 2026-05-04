using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Wallet;

/// <summary>
/// Verifies that wallet balances and transactions created in Shop A are never
/// visible to Shop B, and that mutations from Shop B cannot affect Shop A's data.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "TenantIsolation")]
public sealed class WalletTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task GetBalance_ShopBCannotReadShopACustomerBalance_ReturnsNullOrEmpty()
    {
        // ── Arrange: create a customer and credit their wallet as Shop A ──────
        var shopAClient = fixture.CreateAuthenticatedClient(shopId: 1);
        var suffix      = Guid.NewGuid().ToString("N")[..8];

        var createCustomerResp = await shopAClient.PostAsJsonAsync("/api/crm/customers", new
        {
            DisplayName  = $"ShopA Wallet Customer {suffix}",
            CustomerType = "RETAIL",
            Email        = $"shopa-wallet-{suffix}@test.local",
            Phone        = (string?)null,
            GstNumber    = (string?)null,
            CreditLimit  = 0m,
            GroupId      = (long?)null
        });
        createCustomerResp.IsSuccessStatusCode.Should().BeTrue("setup: Shop A customer must be created");

        // BaseController.Ok<T>(Result<T>) returns OkObjectResult(result.Value) — raw long.
        var custJson   = await createCustomerResp.Content.ReadFromJsonAsync<JsonElement>();
        var customerId = custJson.GetInt64();

        var creditResp = await shopAClient.PostAsJsonAsync("/api/wallet/credit", new
        {
            CustomerId    = customerId,
            CustomerName  = $"ShopA Wallet Customer {suffix}",
            Amount        = 1000m,
            ReferenceType = "Manual",
            ReferenceId   = (long?)null,
            Notes         = "Tenant isolation seed"
        });
        creditResp.IsSuccessStatusCode.Should().BeTrue("setup: wallet credit must succeed");

        // ── Act: Shop B tries to read Shop A's customer balance ───────────────
        var shopBClient  = fixture.CreateAuthenticatedClient(shopId: 2);
        var balanceResp  = await shopBClient.GetAsync($"/api/wallet/balance/{customerId}");

        // ── Assert: 404 — Shop B must not find Shop A's wallet balance ────────
        // The global query filter (ShopId = tenant.ShopId) means WalletBalance
        // for customerId belonging to Shop A will not be found by Shop B.
        balanceResp.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "the global query filter on ShopId must prevent Shop B from reading Shop A's balance");
    }
}
