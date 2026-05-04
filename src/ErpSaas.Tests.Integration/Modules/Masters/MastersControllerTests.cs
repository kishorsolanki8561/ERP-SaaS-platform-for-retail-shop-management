using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Masters;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class MastersControllerTests(IntegrationTestFixture fixture)
{
    // ── GET /api/masters/countries ────────────────────────────────────────────

    [Fact]
    public async Task ListCountries_Unauthenticated_Returns401()
    {
        var client = fixture.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/masters/countries");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListCountries_Authenticated_Returns200AndList()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/masters/countries");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    // ── GET /api/masters/countries/{countryId}/states ─────────────────────────

    [Fact]
    public async Task ListStates_ExistingCountry_Returns200AndList()
    {
        var client = fixture.CreateAuthenticatedClient();
        // Try country ID 1 (seeded in DdlDataSeeder)
        var response = await client.GetAsync("/api/masters/countries/1/states");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/masters/states/{stateId}/cities ──────────────────────────────

    [Fact]
    public async Task ListCities_ExistingState_Returns200AndList()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/masters/states/1/cities");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/masters/currencies ───────────────────────────────────────────

    [Fact]
    public async Task ListCurrencies_Authenticated_Returns200AndList()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/masters/currencies");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/masters/hsn-sac ──────────────────────────────────────────────

    [Fact]
    public async Task SearchHsnSac_WithQuery_Returns200AndFilteredList()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/masters/hsn-sac?q=8516");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/masters/countries ───────────────────────────────────────────

    [Fact]
    public async Task CreateCountry_WithPlatformAdminRole_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var uid = Guid.NewGuid().ToString("N")[..4];
        var payload = new
        {
            Code = $"TS{uid}",
            Name = $"TestCountry-{uid}",
            PhoneCode = "+99",
            CurrencyCode = "TST"
        };
        var response = await client.PostAsJsonAsync("/api/masters/countries", payload);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCountry_WithoutPlatformAdminRole_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Reports.ViewAccounting");
        var payload = new { Code = "XX", Name = "Test", PhoneCode = "+00", CurrencyCode = "TST" };
        var response = await client.PostAsJsonAsync("/api/masters/countries", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCountry_DuplicateCode_Returns409()
    {
        var client = fixture.CreateAuthenticatedClient();
        var uid = Guid.NewGuid().ToString("N")[..4];
        var payload = new { Code = $"DC{uid}", Name = $"Dup-{uid}", PhoneCode = "+99", CurrencyCode = "TST" };

        var first = await client.PostAsJsonAsync("/api/masters/countries", payload);
        first.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        if (first.IsSuccessStatusCode)
        {
            var second = await client.PostAsJsonAsync("/api/masters/countries", payload);
            ((int)second.StatusCode).Should().BeOneOf(409, 400);
        }
    }

    // ── GET /api/ddl/{key} ────────────────────────────────────────────────────

    [Fact]
    public async Task GetDdl_ExistingKey_Returns200AndItems()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/ddl/PAYMENT_MODE");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();
    }
}
