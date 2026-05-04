using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Xunit;

namespace ErpSaas.Tests.Integration.Modules.Crm;

[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Module", "Crm")]
public class CrmControllerTests(IntegrationTestFixture fixture)
{
    // ── GET /api/crm/customers ────────────────────────────────────────────────

    [Fact]
    public async Task ListCustomers_Unauthenticated_Returns401()
    {
        var client = fixture.CreateUnauthenticatedClient();

        var response = await client.GetAsync("/api/crm/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListCustomers_WithoutPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(shopId: 1, permissionCode: "None.None");

        var response = await client.GetAsync("/api/crm/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListCustomers_WithPermission_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["Crm.View"]);

        var response = await client.GetAsync("/api/crm/customers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
    }

    // ── POST /api/crm/customers ───────────────────────────────────────────────

    [Fact]
    public async Task CreateCustomer_ValidDto_Returns200WithId()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["*"]);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var dto = new
        {
            DisplayName = $"Test Customer {suffix}",
            CustomerType = "RETAIL",
            Email = $"cust{suffix}@test.com",
            Phone = (string?)null,
            GstNumber = (string?)null,
            CreditLimit = 5000m,
            GroupId = (long?)null
        };

        var response = await client.PostAsJsonAsync("/api/crm/customers", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<long>();
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateCustomer_WithoutPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(shopId: 1, permissionCode: "Crm.View");
        var dto = new
        {
            DisplayName = "Should Not Create",
            CustomerType = "RETAIL",
            CreditLimit = 0m
        };

        var response = await client.PostAsJsonAsync("/api/crm/customers", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/crm/customers/{id} ───────────────────────────────────────────

    [Fact]
    public async Task GetCustomer_UnknownId_Returns404()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["Crm.View"]);

        var response = await client.GetAsync("/api/crm/customers/999999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /api/crm/customers/{id} ───────────────────────────────────────────

    [Fact]
    public async Task UpdateCustomer_UnknownId_Returns404()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["*"]);
        var dto = new
        {
            DisplayName = "Updated",
            Email = (string?)null,
            Phone = (string?)null,
            GstNumber = (string?)null,
            CreditLimit = 1000m,
            GroupId = (long?)null
        };

        var response = await client.PutAsJsonAsync("/api/crm/customers/999999999", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/crm/customers/{id} ────────────────────────────────────────

    [Fact]
    public async Task DeactivateCustomer_UnknownId_Returns404()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["*"]);

        var response = await client.DeleteAsync("/api/crm/customers/999999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeactivateCustomer_ExistingId_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["*"]);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        // Create a customer first
        var createDto = new
        {
            DisplayName = $"Deactivate Me {suffix}",
            CustomerType = "RETAIL",
            Email = (string?)null,
            Phone = (string?)null,
            GstNumber = (string?)null,
            CreditLimit = 0m,
            GroupId = (long?)null
        };
        var createResponse = await client.PostAsJsonAsync("/api/crm/customers", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var customerId = await createResponse.Content.ReadFromJsonAsync<long>();
        customerId.Should().BeGreaterThan(0);

        // Deactivate the created customer
        var response = await client.DeleteAsync($"/api/crm/customers/{customerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/crm/groups ───────────────────────────────────────────────────

    [Fact]
    public async Task ListGroups_WithPermission_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["Crm.View"]);

        var response = await client.GetAsync("/api/crm/groups");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    // ── POST /api/crm/groups ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateGroup_ValidRequest_Returns200WithId()
    {
        var client = fixture.CreateAuthenticatedClient(shopId: 1, permissions: ["*"]);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var req = new
        {
            Code = $"GRP{suffix}",
            Name = $"Test Group {suffix}",
            DiscountPercent = 5m
        };

        var response = await client.PostAsJsonAsync("/api/crm/groups", req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<long>();
        id.Should().BeGreaterThan(0);
    }
}
