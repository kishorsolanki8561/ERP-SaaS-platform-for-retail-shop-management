using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Wallet;

/// <summary>
/// Verifies that wallet balances and transactions created in Shop A are never
/// visible to Shop B, and that mutations from Shop B cannot affect Shop A's
/// wallet data.
///
/// Full implementation requires <c>IntegrationTestFixture</c> with two
/// pre-onboarded shops — deferred to Phase 1.
/// </summary>
[Trait("Category", "Integration")]
public class WalletTenantIsolationTests
{
    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task ListBalances_ShopA_DoesNotReturnShopBBalances()
    {
        // Arrange: credit customer in ShopA, authenticate as ShopB
        // Act: GET /api/wallet/balances
        // Assert: result does not contain ShopA balance
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task GetBalance_ShopBCannotReadShopABalance_Returns404()
    {
        // Arrange: credit customer in ShopA, get customerId
        // Act: GET /api/wallet/balances/{customerId} authenticated as ShopB
        // Assert: 404 (not 403 — must not leak existence)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task DebitAsync_ShopBCannotDebitShopACustomer_ReturnsConflict()
    {
        // Arrange: credit customer in ShopA, attempt debit as ShopB
        // Act: POST /api/wallet/debit with ShopA customerId as ShopB
        // Assert: 409 (no balance found for ShopB customer)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task ListTransactions_ShopBCannotReadShopATransactions()
    {
        // Arrange: credit then debit in ShopA
        // Act: GET /api/wallet/balances/{customerId}/transactions as ShopB
        // Assert: empty list or 404
        await Task.CompletedTask;
    }
}
