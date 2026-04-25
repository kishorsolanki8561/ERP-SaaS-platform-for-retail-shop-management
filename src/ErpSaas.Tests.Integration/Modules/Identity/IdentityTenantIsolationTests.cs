using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Identity;

/// <summary>
/// Verifies that identity data (users, roles, branches) belonging to Shop A is
/// never visible to Shop B, and that Shop B cannot mutate Shop A's identity
/// records.
///
/// Full implementation requires <c>IntegrationTestFixture</c> with two
/// pre-onboarded shops — deferred to Phase 1.
/// </summary>
[Trait("Category", "Integration")]
public class IdentityTenantIsolationTests
{
    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task ListUsers_ShopA_DoesNotReturnShopBUsers()
    {
        // Arrange: invite user in ShopA, authenticate as ShopB
        // Act: GET /api/admin/users
        // Assert: ShopA user not in result
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task DeactivateUser_ShopBCannotDeactivateShopAUser_Returns404()
    {
        // Arrange: active user in ShopA
        // Act: PUT /api/admin/users/{shopA_userId}/deactivate as ShopB
        // Assert: 404
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task ListRoles_ShopA_DoesNotReturnShopBCustomRoles()
    {
        // Arrange: custom role in ShopA, authenticate as ShopB
        // Act: GET /api/admin/roles
        // Assert: ShopA role not in result (system roles shared, shop roles isolated)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task GetShopProfile_ShopBCannotReadShopAProfile_ReturnsShopBProfile()
    {
        // Each shop's admin gets only their own shop's profile.
        // Arrange: two shops with different legal names
        // Act: GET /api/admin/shop-profile as ShopB admin
        // Assert: returns ShopB's LegalName, not ShopA's
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task ListBranches_ShopA_DoesNotReturnShopBBranches()
    {
        // Arrange: branches in both ShopA and ShopB
        // Act: GET /api/admin/branches as ShopA
        // Assert: only ShopA branches returned
        await Task.CompletedTask;
    }
}
