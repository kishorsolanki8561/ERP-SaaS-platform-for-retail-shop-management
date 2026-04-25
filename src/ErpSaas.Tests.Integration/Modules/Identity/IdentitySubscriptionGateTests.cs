using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Identity;

/// <summary>
/// Verifies subscription-gating behaviour for Identity / Admin features.
///
/// Core identity operations (login, profile, user list) are available on all
/// plans.  Advanced features like SSO or extended role management may require
/// higher-tier plans.
///
/// Full implementation requires <c>IntegrationTestFixture</c> + subscription
/// plan seeding — deferred to Phase 1.
/// </summary>
[Trait("Category", "Integration")]
public class IdentitySubscriptionGateTests
{
    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task Login_AllPlans_Returns200()
    {
        // Login must work regardless of subscription tier.
        // Arrange: shop on Starter plan
        // Act: POST /api/auth/login
        // Assert: 200
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task ListUsers_AllPlans_Returns200()
    {
        // User management is available on all plans.
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task SsoLogin_StarterPlan_Returns402()
    {
        // SSO / SAML is an Enterprise-only feature.
        // Arrange: shop on Starter plan
        // Act: POST /api/auth/sso
        // Assert: 402 Payment Required
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture + plan seeding — Phase 1")]
    public async Task InviteUser_StarterPlanAtUserLimit_Returns402()
    {
        // Starter plan has MaxUsers = 5.
        // Arrange: shop already at 5 active users
        // Act: POST /api/admin/users/invite (6th user)
        // Assert: 402 (quota exceeded)
        await Task.CompletedTask;
    }
}
