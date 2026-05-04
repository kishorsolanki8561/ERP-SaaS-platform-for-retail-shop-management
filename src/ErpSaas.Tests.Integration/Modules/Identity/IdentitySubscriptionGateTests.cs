using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Identity;

/// <summary>
/// Verifies subscription-gating behaviour for Identity / Admin features.
///
/// Core identity operations (login, user list, roles) carry no [RequireFeature]
/// attribute and are therefore accessible on all subscription plans. These tests
/// confirm that removing the "feats" claim from the JWT does NOT gate any
/// core identity endpoint (HTTP 200, not 402/403).
///
/// Advanced features (SSO, seat quotas) are gated but those endpoints do not
/// yet exist in the code-base (planned for Phase 7), so those tests document
/// the intended behaviour without exercising non-existent routes.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class IdentitySubscriptionGateTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Login_AllPlans_Returns200()
    {
        // Login (/api/auth/login) is [AllowAnonymous] — no subscription gate at all.
        // A freshly seeded user can always log in regardless of features.
        var (_, email, password) = await fixture.SeedTestUserAsync();
        var client  = fixture.CreateUnauthenticatedClient();
        var payload = new { Identifier = email, Password = password };

        var response = await client.PostAsJsonAsync("/api/auth/login", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Login must succeed on all plans — it has no feature gate");
    }

    [Fact]
    public async Task ListUsers_AllPlans_Returns200()
    {
        // /api/admin/users uses [RequirePermission("Users.View")] but has NO [RequireFeature].
        // A client authenticated with "feats" claim = empty still gets 200.
        // CreateNoFeatureClient produces a JWT with is_platform_admin=true but no feats claim.
        var client   = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "User management must be available on all plans — no feature gate");
    }

    [Fact]
    public async Task ListRoles_AllPlans_Returns200()
    {
        // Same check for roles — no feature gate on this endpoint.
        var client   = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/admin/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Role listing must be available on all plans — no feature gate");
    }

    [Fact]
    public async Task GetShopProfile_AllPlans_ReturnsNot402()
    {
        // Shop profile is core functionality — no feature gate.
        // Response is either 200 (shop exists) or 404 (no shop for shopId=1 seed).
        // What must NOT happen is 402 (payment required from feature gate).
        var client   = fixture.CreateNoFeatureClient();
        var response = await client.GetAsync("/api/admin/shop-profile");

        response.StatusCode.Should().NotBe(HttpStatusCode.PaymentRequired,
            "Shop profile must not be behind a subscription gate");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
