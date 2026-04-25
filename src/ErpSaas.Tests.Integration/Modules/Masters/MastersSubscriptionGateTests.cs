using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Masters;

/// <summary>
/// Verifies subscription-gating behaviour for Masters features.
///
/// Master data (countries, states, currencies, HSN codes) is read-only for all
/// plans and should always return 200.  Write operations on master data are
/// platform-admin only and bypass subscription gating.
///
/// Full implementation requires <c>IntegrationTestFixture</c> + subscription
/// plan seeding — deferred to Phase 1.
/// </summary>
[Trait("Category", "Integration")]
public class MastersSubscriptionGateTests
{
    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task ListCountries_AllPlans_Returns200()
    {
        // Master data reads are not gated by subscription.
        // Arrange: shop on Starter plan
        // Act: GET /api/masters/countries
        // Assert: 200
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task SearchHsnSac_AllPlans_Returns200()
    {
        // Arrange: shop on any plan
        // Act: GET /api/masters/hsn?q=8516
        // Assert: 200
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task ListCurrencies_AllPlans_Returns200()
    {
        // Arrange: shop on Starter plan
        // Act: GET /api/masters/currencies
        // Assert: 200
        await Task.CompletedTask;
    }
}
