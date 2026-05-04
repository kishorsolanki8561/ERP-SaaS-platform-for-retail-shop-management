using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Wallet;

/// <summary>
/// Verifies subscription-gating behaviour for Wallet features.
///
/// The WalletController does not currently carry [RequireFeature] attributes —
/// the feature gate is enforced at the menu/route level only.  These tests
/// verify that:
/// 1. Without auth the endpoint returns 401 (baseline).
/// 2. With full auth + all features the endpoint returns 200.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "SubscriptionGate")]
public sealed class WalletSubscriptionGateTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task WalletBalances_Unauthenticated_Returns401()
    {
        var client   = fixture.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/wallet/balances");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WalletCredit_AllPlans_Returns200WhenAuthenticated()
    {
        // WalletController has no [RequireFeature] — it should return 200 for
        // any authenticated user with the correct permission regardless of plan.
        var adminClient = fixture.CreateAuthenticatedClient();
        var suffix      = Guid.NewGuid().ToString("N")[..8];

        // Create a customer first so we have a valid CustomerId.
        var createCustomerResp = await adminClient.PostAsJsonAsync("/api/crm/customers", new
        {
            DisplayName  = $"SubscGate Wallet {suffix}",
            CustomerType = "RETAIL",
            Email        = $"gate-wallet-{suffix}@test.local",
            Phone        = (string?)null,
            GstNumber    = (string?)null,
            CreditLimit  = 0m,
            GroupId      = (long?)null
        });
        createCustomerResp.IsSuccessStatusCode.Should().BeTrue();

        // BaseController.Ok<T>(Result<T>) returns OkObjectResult(result.Value) — raw long.
        var custJson   = await createCustomerResp.Content.ReadFromJsonAsync<JsonElement>();
        var customerId = custJson.GetInt64();

        var creditBody = new
        {
            CustomerId    = customerId,
            CustomerName  = $"SubscGate Wallet {suffix}",
            Amount        = 100m,
            ReferenceType = (string?)null,
            ReferenceId   = (long?)null,
            Notes         = (string?)null
        };

        var response = await adminClient.PostAsJsonAsync("/api/wallet/credit", creditBody);

        // No [RequireFeature] on WalletController so authenticated + permissioned
        // users always succeed regardless of feature flags.
        response.IsSuccessStatusCode.Should().BeTrue(
            "WalletController has no [RequireFeature] gate — all plans can access it");
    }
}
