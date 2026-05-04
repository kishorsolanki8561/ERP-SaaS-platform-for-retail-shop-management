using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Hr;

/// <summary>
/// Verifies that HR data created in Shop A is never visible to Shop B.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "TenantIsolation")]
public sealed class HrTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task ListEmployees_ShopA_DoesNotReturnShopBEmployees()
    {
        // ── Arrange: create an employee in Shop B ─────────────────────────────
        var shopBClient = fixture.CreateAuthenticatedClient(shopId: 2);
        var suffix      = Guid.NewGuid().ToString("N")[..8];

        var createEmpResp = await shopBClient.PostAsJsonAsync("/api/employees", new
        {
            FirstName         = $"ShopBEmp{suffix}",
            LastName          = "TenantIsolation",
            Phone             = (string?)null,
            Email             = $"shopb-emp-{suffix}@test.local",
            DateOfBirth       = new DateTime(1991, 8, 10),
            DateOfJoining     = new DateTime(2021, 4, 1),
            Designation       = "Accountant",
            Department        = "Finance",
            BasicSalary       = 22000m,
            BankAccountNumber = (string?)null,
            BankIfsc          = (string?)null,
            PanNumber         = (string?)null,
            LinkedUserId      = (long?)null
        });
        createEmpResp.IsSuccessStatusCode.Should().BeTrue("setup: Shop B employee creation must succeed");

        // ── Act: list employees as Shop A ─────────────────────────────────────
        var shopAClient      = fixture.CreateAuthenticatedClient(shopId: 1);
        var listResponse     = await shopAClient.GetAsync("/api/employees");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // EmployeeService.CreateAsync returns Result<long> → OkObjectResult(id) → plain long.
        var shopBJson     = await createEmpResp.Content.ReadFromJsonAsync<JsonElement>();
        var shopBEmpId    = shopBJson.GetInt64();

        var listJson      = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        // The response is a JSON array — parse and collect all returned IDs.
        var returnedIds   = new List<long>();
        if (listJson.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in listJson.EnumerateArray())
                returnedIds.Add(item.GetProperty("id").GetInt64());
        }

        // ── Assert: Shop A's list must not contain Shop B's employee ──────────
        returnedIds.Should().NotContain(shopBEmpId,
            "global query filter on ShopId must prevent cross-tenant employee visibility");
    }

    [Fact]
    public async Task GetEmployee_ShopBCannotReadShopAEmployee_Returns404()
    {
        // ── Arrange: create an employee in Shop A ─────────────────────────────
        var shopAClient = fixture.CreateAuthenticatedClient(shopId: 1);
        var suffix      = Guid.NewGuid().ToString("N")[..8];

        var createEmpResp = await shopAClient.PostAsJsonAsync("/api/employees", new
        {
            FirstName         = $"ShopAEmp{suffix}",
            LastName          = "CrossRead",
            Phone             = (string?)null,
            Email             = $"shopa-emp-{suffix}@test.local",
            DateOfBirth       = new DateTime(1988, 12, 25),
            DateOfJoining     = new DateTime(2019, 9, 15),
            Designation       = "Supervisor",
            Department        = "Operations",
            BasicSalary       = 35000m,
            BankAccountNumber = (string?)null,
            BankIfsc          = (string?)null,
            PanNumber         = (string?)null,
            LinkedUserId      = (long?)null
        });
        createEmpResp.IsSuccessStatusCode.Should().BeTrue("setup: Shop A employee creation must succeed");

        // EmployeeService.CreateAsync returns Result<long> → OkObjectResult(id) → plain long.
        var shopAJson  = await createEmpResp.Content.ReadFromJsonAsync<JsonElement>();
        var employeeId = shopAJson.GetInt64();

        // ── Act: Shop B attempts to read Shop A's employee by ID ──────────────
        var shopBClient = fixture.CreateAuthenticatedClient(shopId: 2);
        var getResponse = await shopBClient.GetAsync($"/api/employees/{employeeId}");

        // ── Assert: 404 — the entity is invisible due to ShopId filter ────────
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "global query filter on ShopId must prevent Shop B from reading Shop A's employee");
    }
}
