using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Xunit;

namespace ErpSaas.Tests.Integration.Modules.Crm;

/// <summary>
/// Seeds two shops; asserts that no reads or writes from Shop A bleed into Shop B.
/// Verifies global query filter on ShopId is enforced for Customer.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Module", "Crm")]
public class CrmTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task ListCustomers_ShopA_DoesNotReturnShopBCustomers()
    {
        // Arrange: seed Shop A and Shop B with separate users
        var (shopAId, _, _) = await fixture.SeedTestUserAsync();
        var (shopBId, _, _) = await fixture.SeedTestUserAsync();

        var shopAClient = fixture.CreateAuthenticatedClient(shopId: shopAId, permissions: ["*"]);
        var shopBClient = fixture.CreateAuthenticatedClient(shopId: shopBId, permissions: ["*"]);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        // Create a customer in Shop A
        var createDto = new
        {
            DisplayName = $"ShopA Customer {suffix}",
            CustomerType = "RETAIL",
            Email = (string?)null,
            Phone = (string?)null,
            GstNumber = (string?)null,
            CreditLimit = 0m,
            GroupId = (long?)null
        };
        var createResponse = await shopAClient.PostAsJsonAsync("/api/crm/customers", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var shopACustomerId = await createResponse.Content.ReadFromJsonAsync<long>();

        // Act: list customers as Shop B
        var listResponse = await shopBClient.GetAsync("/api/crm/customers");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("items");

        // Assert: Shop A's customer is not in Shop B's list
        var ids = items.EnumerateArray()
            .Select(item => item.GetProperty("id").GetInt64())
            .ToList();
        ids.Should().NotContain(shopACustomerId,
            because: "Shop B must not see Shop A's customers");
    }

    [Fact]
    public async Task GetCustomer_ShopBCannotReadShopACustomer_Returns404()
    {
        // Arrange: create a customer in Shop A
        var (shopAId, _, _) = await fixture.SeedTestUserAsync();
        var (shopBId, _, _) = await fixture.SeedTestUserAsync();

        var shopAClient = fixture.CreateAuthenticatedClient(shopId: shopAId, permissions: ["*"]);
        var shopBClient = fixture.CreateAuthenticatedClient(shopId: shopBId, permissions: ["*"]);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createDto = new
        {
            DisplayName = $"ShopA Private Customer {suffix}",
            CustomerType = "RETAIL",
            Email = (string?)null,
            Phone = (string?)null,
            GstNumber = (string?)null,
            CreditLimit = 0m,
            GroupId = (long?)null
        };
        var createResponse = await shopAClient.PostAsJsonAsync("/api/crm/customers", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var shopACustomerId = await createResponse.Content.ReadFromJsonAsync<long>();

        // Act: Shop B tries to GET Shop A's customer by ID
        var response = await shopBClient.GetAsync($"/api/crm/customers/{shopACustomerId}");

        // Assert: 404 — must not leak existence across tenant boundary
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCustomer_ShopBCannotUpdateShopACustomer_Returns404()
    {
        // Arrange: create a customer in Shop A
        var (shopAId, _, _) = await fixture.SeedTestUserAsync();
        var (shopBId, _, _) = await fixture.SeedTestUserAsync();

        var shopAClient = fixture.CreateAuthenticatedClient(shopId: shopAId, permissions: ["*"]);
        var shopBClient = fixture.CreateAuthenticatedClient(shopId: shopBId, permissions: ["*"]);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createDto = new
        {
            DisplayName = $"ShopA Target Customer {suffix}",
            CustomerType = "RETAIL",
            Email = (string?)null,
            Phone = (string?)null,
            GstNumber = (string?)null,
            CreditLimit = 0m,
            GroupId = (long?)null
        };
        var createResponse = await shopAClient.PostAsJsonAsync("/api/crm/customers", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var shopACustomerId = await createResponse.Content.ReadFromJsonAsync<long>();

        // Act: Shop B tries to update Shop A's customer
        var updateDto = new
        {
            DisplayName = "Hijacked Name",
            Email = (string?)null,
            Phone = (string?)null,
            GstNumber = (string?)null,
            CreditLimit = 0m,
            GroupId = (long?)null
        };
        var response = await shopBClient.PutAsJsonAsync($"/api/crm/customers/{shopACustomerId}", updateDto);

        // Assert: 404 — Shop B cannot modify Shop A's data
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
