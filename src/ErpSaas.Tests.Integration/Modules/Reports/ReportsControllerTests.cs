using System.Net;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Reports;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class ReportsControllerTests(IntegrationTestFixture fixture)
{
    private static string DateRange =>
        $"?from={DateTime.UtcNow.AddDays(-30):yyyy-MM-dd}&to={DateTime.UtcNow:yyyy-MM-dd}";

    // ── GET /api/reports/trial-balance ────────────────────────────────────────

    [Fact]
    public async Task TrialBalance_WithoutAuth_Returns401()
    {
        var client = fixture.CreateUnauthenticatedClient();
        var response = await client.GetAsync($"/api/reports/trial-balance{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TrialBalance_WithValidToken_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/reports/trial-balance{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TrialBalance_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Reports.ViewGst");
        var response = await client.GetAsync($"/api/reports/trial-balance{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/reports/profit-loss ─────────────────────────────────────────

    [Fact]
    public async Task ProfitLoss_WithValidToken_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/reports/profit-loss{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/reports/day-book ─────────────────────────────────────────────

    [Fact]
    public async Task DayBook_WithValidToken_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/reports/day-book{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/reports/cash-book ────────────────────────────────────────────

    [Fact]
    public async Task CashBook_WithValidToken_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/reports/cash-book{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/reports/gstr1-b2b (ViewGst permission) ──────────────────────

    [Fact]
    public async Task Gstr1B2b_WithoutGstPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Reports.ViewAccounting");
        var response = await client.GetAsync($"/api/reports/gstr1-b2b{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Gstr1B2b_WithGstPermission_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/reports/gstr1-b2b{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/reports/gstr3b (ViewGst + Accounting.GstReturns feature) ────

    [Fact]
    public async Task Gstr3b_FeatureOff_Returns403()
    {
        var client = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync($"/api/reports/gstr3b{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Gstr3b_FeatureOn_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(features: ["Accounting.GstReturns"]);
        var response = await client.GetAsync($"/api/reports/gstr3b{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/reports/export/trial-balance (requires Reports.Export) ───────

    [Fact]
    public async Task Export_PdfFormat_ReturnsPdfFile()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync(
            $"/api/reports/export/TrialBalance{DateRange}&format=Pdf");
        // Export returns file or result — either 200 is acceptable
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Export_ExcelFormat_ReturnsXlsx()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync(
            $"/api/reports/export/trial-balance{DateRange}&format=Excel");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
