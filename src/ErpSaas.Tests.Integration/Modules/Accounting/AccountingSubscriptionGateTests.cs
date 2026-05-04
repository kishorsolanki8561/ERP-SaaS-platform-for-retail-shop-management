using System.Net;
using System.Net.Http.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Accounting;

/// <summary>
/// Verifies that feature-gated Accounting endpoints return 403 when the shop's plan
/// does not include the required feature, and succeed when it does.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "SubscriptionGate")]
public class AccountingSubscriptionGateTests(IntegrationTestFixture fixture)
{
    // ── POST /api/accounting/financial-years/{id}/close  [RequireFeature("Accounting.Basic")] ──

    [Fact]
    public async Task CloseFinancialYear_FeatureOff_Returns403()
    {
        // Arrange: client has all permissions but NO feature flags
        var noFeatClient = fixture.CreateNoFeatureClient(shopId: 1);

        // Create a year first using a client that has the feature
        var adminClient = fixture.CreateAuthenticatedClient(
            shopId: 1,
            permissions: ["*"],
            features: ["Accounting.Basic"]);
        var createResp = await adminClient.PostAsJsonAsync("/api/accounting/financial-years",
            new { StartYear = 2020 });
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var yearId = await createResp.Content.ReadFromJsonAsync<long>();

        // Act: try to close without feature flag
        var response = await noFeatClient.PostAsync(
            $"/api/accounting/financial-years/{yearId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CloseFinancialYear_FeatureOn_Passes()
    {
        // Arrange: client has feature "Accounting.Basic"
        var client = fixture.CreateAuthenticatedClient(
            shopId: 1,
            permissions: ["*"],
            features: ["Accounting.Basic"]);

        // Create a fresh financial year to close
        var createResp = await client.PostAsJsonAsync("/api/accounting/financial-years",
            new { StartYear = 2019 });
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var yearId = await createResp.Content.ReadFromJsonAsync<long>();

        // Act: close with feature on
        var response = await client.PostAsync(
            $"/api/accounting/financial-years/{yearId}/close", null);

        // Not 403 — may be 200 or another business error, but feature gate passed
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ── GET /api/accounting/bank-statements  [RequireFeature("Accounting.Advanced")] ──

    [Fact]
    public async Task BankReconciliation_FeatureOff_Returns403()
    {
        // Arrange: no feature claims
        var noFeatClient = fixture.CreateNoFeatureClient(shopId: 1);

        var response = await noFeatClient.GetAsync("/api/accounting/bank-statements");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BankReconciliation_FeatureOn_Returns200()
    {
        // Arrange: client has "Accounting.Advanced" feature
        var client = fixture.CreateAuthenticatedClient(
            shopId: 1,
            permissions: ["*"],
            features: ["Accounting.Advanced"]);

        var response = await client.GetAsync("/api/accounting/bank-statements");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
