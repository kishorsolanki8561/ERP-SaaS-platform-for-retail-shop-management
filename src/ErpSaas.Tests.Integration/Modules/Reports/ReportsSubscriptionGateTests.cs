using System.Net;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Reports;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class ReportsSubscriptionGateTests(IntegrationTestFixture fixture)
{
    private static string DateRange =>
        $"?from={DateTime.UtcNow.AddDays(-30):yyyy-MM-dd}&to={DateTime.UtcNow:yyyy-MM-dd}";

    [Fact]
    public async Task Reports_FeatureOff_Returns403_ForGstr3bOnly()
    {
        // Only gstr3b has [RequireFeature("Accounting.GstReturns")] — other reports are ungated
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync($"/api/reports/gstr3b{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reports_FeatureOn_Returns200_ForGstr3b()
    {
        var client = fixture.CreateAuthenticatedClient(features: ["Accounting.GstReturns"]);
        var response = await client.GetAsync($"/api/reports/gstr3b{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TrialBalance_NoFeatureClaim_Returns200()
    {
        // trial-balance has no [RequireFeature] — available on all plans
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync($"/api/reports/trial-balance{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CashBook_NoFeatureClaim_Returns200()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync($"/api/reports/cash-book{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
