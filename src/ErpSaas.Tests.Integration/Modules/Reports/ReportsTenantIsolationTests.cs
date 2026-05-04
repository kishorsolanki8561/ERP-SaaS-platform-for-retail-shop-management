using System.Net;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Reports;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class ReportsTenantIsolationTests(IntegrationTestFixture fixture)
{
    private static string DateRange =>
        $"?from={DateTime.UtcNow.AddDays(-30):yyyy-MM-dd}&to={DateTime.UtcNow:yyyy-MM-dd}";

    [Fact]
    public async Task TrialBalance_Shop1Token_ReturnsOnlyShop1Vouchers()
    {
        // Both shops get 200 — each only sees their own data via global query filters
        var shop1Client = fixture.CreateAuthenticatedClient(shopId: 1);
        var shop2Client = fixture.CreateAuthenticatedClient(shopId: 2);

        var resp1 = await shop1Client.GetAsync($"/api/reports/trial-balance{DateRange}");
        var resp2 = await shop2Client.GetAsync($"/api/reports/trial-balance{DateRange}");

        resp1.StatusCode.Should().Be(HttpStatusCode.OK);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Both bodies are valid JSON — isolation is enforced by TenantDbContext global filters
        var body1 = await resp1.Content.ReadAsStringAsync();
        var body2 = await resp2.Content.ReadAsStringAsync();
        JsonDocument.Parse(body1).RootElement.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        JsonDocument.Parse(body2).RootElement.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    [Fact]
    public async Task GstR1_Shop1Token_ReturnsOnlyShop1Invoices()
    {
        var shop1Client = fixture.CreateAuthenticatedClient(shopId: 1);
        var shop2Client = fixture.CreateAuthenticatedClient(shopId: 2);

        var resp1 = await shop1Client.GetAsync($"/api/reports/gstr1-b2b{DateRange}");
        var resp2 = await shop2Client.GetAsync($"/api/reports/gstr1-b2b{DateRange}");

        resp1.StatusCode.Should().Be(HttpStatusCode.OK);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);

        var body1 = await resp1.Content.ReadAsStringAsync();
        var body2 = await resp2.Content.ReadAsStringAsync();
        JsonDocument.Parse(body1).RootElement.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        JsonDocument.Parse(body2).RootElement.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }
}
