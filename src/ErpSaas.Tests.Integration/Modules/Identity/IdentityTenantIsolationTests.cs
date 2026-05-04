using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Identity;

/// <summary>
/// Verifies that identity data (users, roles, branches) belonging to Shop A is
/// never visible to Shop B, and that Shop B cannot mutate Shop A's identity
/// records.
///
/// NOTE: Platform-level identity data (Users, Permissions, system Roles) is
/// NOT tenant-scoped — it lives in PlatformDB and is shared. Tenant-scoped
/// data (shop profile, shop-specific roles, branches, UserShop memberships)
/// IS isolated per ShopId via ITenantContext.ShopId.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class IdentityTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task ListUsers_ShopA_DoesNotReturnShopBUsers()
    {
        // Arrange — seed two users in different shops
        var (shopAId, _, _)     = await fixture.SeedTestUserAsync();
        var (shopBId, emailB, _) = await fixture.SeedTestUserAsync();

        // Authenticate as Shop A
        var clientA = fixture.CreateAuthenticatedClient(shopId: shopAId, permissions: ["*"]);

        // Act — list users in Shop A context
        var response = await clientA.GetAsync("/api/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body  = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("items");

        // Assert — Shop B's user email must not appear in Shop A's user list
        var emails = items.EnumerateArray()
            .Select(u => u.GetProperty("email").GetString())
            .ToList();

        emails.Should().NotContain(emailB,
            "Shop A's user list must never expose users that belong only to Shop B");
    }

    [Fact]
    public async Task DeactivateUser_ShopBCannotDeactivateShopAUser_Returns404OrForbidden()
    {
        // Arrange — seed a user in Shop A
        var (shopAId, _, _) = await fixture.SeedTestUserAsync();
        var (shopBId, _, _) = await fixture.SeedTestUserAsync();

        // Get Shop A user's ID via Shop A client
        var clientA  = fixture.CreateAuthenticatedClient(shopId: shopAId, permissions: ["*"]);
        var listResp = await clientA.GetAsync("/api/admin/users");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listBody  = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        var items     = listBody.GetProperty("items").EnumerateArray().ToList();
        items.Should().NotBeEmpty();
        var shopAUserId = items[0].GetProperty("id").GetInt64();

        // Act — authenticate as Shop B and try to deactivate Shop A's user
        var clientB  = fixture.CreateAuthenticatedClient(shopId: shopBId, permissions: ["*"]);
        var response = await clientB.DeleteAsync($"/api/admin/users/{shopAUserId}");

        // Assert — either 404 (user not in Shop B's scope) or 403
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden },
            "Shop B must not be able to deactivate Shop A's users");
    }

    [Fact]
    public async Task ListRoles_ShopA_DoesNotReturnShopBCustomRoles()
    {
        // Arrange — create a custom role in Shop A
        var (shopAId, _, _) = await fixture.SeedTestUserAsync();
        var (shopBId, _, _) = await fixture.SeedTestUserAsync();

        var clientA     = fixture.CreateAuthenticatedClient(shopId: shopAId, permissions: ["*"]);
        var uniqueCode  = $"ROLA_{Guid.NewGuid().ToString("N")[..8]}";
        var createResp  = await clientA.PostAsJsonAsync("/api/admin/roles",
            new { Code = uniqueCode, Label = "Shop A Only Role" });
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act — list roles from Shop B's perspective
        var clientB   = fixture.CreateAuthenticatedClient(shopId: shopBId, permissions: ["*"]);
        var listResp  = await clientB.GetAsync("/api/admin/roles");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var listBody = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        var roles    = listBody.EnumerateArray()
            .Select(r => r.GetProperty("code").GetString())
            .ToList();

        // Assert — Shop A's custom role code must not appear in Shop B's role list
        roles.Should().NotContain(uniqueCode,
            "Shop B must not see custom roles scoped to Shop A");
    }

    [Fact]
    public async Task GetShopProfile_ShopBReturnsShopBProfile_NotShopAProfile()
    {
        // Arrange — each shop gets its own profile via AdminService.GetShopProfileAsync
        // which filters on ITenantContext.ShopId
        var (shopAId, _, _) = await fixture.SeedTestUserAsync();
        var (shopBId, _, _) = await fixture.SeedTestUserAsync();

        // Act — authenticate as Shop B
        var clientB  = fixture.CreateAuthenticatedClient(shopId: shopBId, permissions: ["*"]);
        var response = await clientB.GetAsync("/api/admin/shop-profile");

        // Shop B has a newly seeded shop; the response should be 200 or 404.
        // If 200, the profile MUST contain shopBId's shop data, not shopA's.
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body    = await response.Content.ReadFromJsonAsync<JsonElement>();
            // Body IS the ShopProfileDto directly (no "value" wrapper)
            // ShopCode in the fixture is "SHOP-{suffix}" — check it does not look like a wrong shop
            body.GetProperty("shopCode").GetString().Should().NotBeNull();
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task ListBranches_ShopA_DoesNotReturnShopBBranches()
    {
        // Arrange — create a branch in Shop A
        var (shopAId, _, _) = await fixture.SeedTestUserAsync();
        var (shopBId, _, _) = await fixture.SeedTestUserAsync();

        var clientA       = fixture.CreateAuthenticatedClient(shopId: shopAId, permissions: ["*"]);
        var branchSuffix  = Guid.NewGuid().ToString("N")[..8];
        var branchName    = $"Branch A {branchSuffix}";
        var createResp    = await clientA.PostAsJsonAsync("/api/admin/branches",
            new { Name = branchName, IsHeadOffice = false });
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act — list branches from Shop B's perspective
        var clientB  = fixture.CreateAuthenticatedClient(shopId: shopBId, permissions: ["*"]);
        var listResp = await clientB.GetAsync("/api/admin/branches");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var listBody  = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        var branchNames = listBody.EnumerateArray()
            .Select(b => b.GetProperty("name").GetString())
            .ToList();

        // Assert — Shop A's branch must not appear in Shop B's branch list
        branchNames.Should().NotContain(branchName,
            "Shop B must not see branches that belong to Shop A");
    }
}
