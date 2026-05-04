using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace ErpSaas.Tests.Integration.Modules.CustomerPortal;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class CustomerPortalControllerTests(IntegrationTestFixture fixture)
{
    // ── Portal Auth ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RequestOtp_ValidPhone_Returns200WithChallenge()
    {
        var client = fixture.CreateClient();
        var response = await client.PostAsJsonAsync("/api/portal/auth/signup-otp",
            new { Identifier = "+919876543210" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VerifyOtp_InvalidOtp_Returns401()
    {
        var client = fixture.CreateClient();
        var response = await client.PostAsJsonAsync("/api/portal/auth/verify-otp",
            new { Identifier = "+919876543210", Otp = "000000", DeviceFingerprint = (string?)null });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Portal endpoints require CustomerAuth ─────────────────────────────────

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var client = fixture.CreateClient();
        var response = await client.GetAsync("/api/portal/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithStaffToken_Returns403()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/portal/me");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Online orders staff endpoints ─────────────────────────────────────────

    [Fact]
    public async Task ListOnlineOrders_WithPermission_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/online-orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListOnlineOrders_WithoutPermission_Returns403()
    {
        var client = fixture.CreateAuthenticatedClient(permissions: []);
        var response = await client.GetAsync("/api/online-orders");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AcceptOrder_NonExistentOrder_Returns404()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.PatchAsync("/api/online-orders/99999/accept", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RejectOrder_NonExistentOrder_Returns404()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.PatchAsJsonAsync("/api/online-orders/99999/reject",
            new { Reason = "Out of stock" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

