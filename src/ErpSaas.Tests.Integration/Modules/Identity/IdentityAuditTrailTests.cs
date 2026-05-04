using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Modules.Identity;

/// <summary>
/// Verifies observable state changes produced by Identity mutations.
///
/// NOTE: <c>AuthService</c> and <c>AdminService</c> do not yet call
/// <c>IAuditLogger.LogAsync</c> explicitly (that is a Phase 5 polish item).
/// <c>AuditSaveChangesInterceptor</c> only stamps <c>CreatedAtUtc</c>/
/// <c>UpdatedAtUtc</c> — it does not write <c>AuditLog</c> rows.
///
/// These tests therefore verify the entity-level state changes that are
/// the direct, observable consequence of each mutation.  When explicit
/// <c>IAuditLogger</c> calls are added in Phase 5, a second assertion
/// block against <c>LogDbContext.AuditLogs</c> should be added.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class IdentityAuditTrailTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Login_SuccessfulLogin_UpdatesLastLoginAtUtc()
    {
        // Arrange
        var (_, email, password) = await fixture.SeedTestUserAsync();
        var client  = fixture.CreateUnauthenticatedClient();
        var beforeLogin = DateTime.UtcNow;

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Identifier = email, Password = password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert — LastLoginAtUtc updated in User row (observable state change)
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        user.Should().NotBeNull();
        user!.LastLoginAtUtc.Should().NotBeNull();
        user.LastLoginAtUtc!.Value.Should().BeAfter(beforeLogin.AddSeconds(-2));
    }

    [Fact]
    public async Task Login_SuccessfulLogin_CreatesRefreshTokenRow()
    {
        // Arrange
        var (_, email, password) = await fixture.SeedTestUserAsync();
        var client  = fixture.CreateUnauthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Identifier = email, Password = password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert — a UserSecurityToken row was created for the refresh token
        await using var scope = fixture.CreateScope();
        var db   = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        user.Should().NotBeNull();

        var hasRefreshToken = await db.UserSecurityTokens
            .AnyAsync(t => t.UserId == user!.Id && t.ConsumedAtUtc == null);
        hasRefreshToken.Should().BeTrue(
            "a successful login must create a refresh-token row in UserSecurityToken");
    }

    [Fact]
    public async Task Login_FailedLogin_IncrementsFailedLoginCount()
    {
        // Arrange
        var (_, email, _) = await fixture.SeedTestUserAsync();
        var client  = fixture.CreateUnauthenticatedClient();

        // Act — wrong password
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Identifier = email, Password = "WrongPassword@99" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Assert — FailedLoginCount incremented
        await using var scope = fixture.CreateScope();
        var db   = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        user.Should().NotBeNull();
        user!.FailedLoginCount.Should().BeGreaterThan(0,
            "a failed login must increment FailedLoginCount on the User row");
    }

    [Fact]
    public async Task InviteUser_CreatesInactiveUserWithInviteToken()
    {
        // Arrange
        var (shopId, _, _)  = await fixture.SeedTestUserAsync();
        var client         = fixture.CreateAuthenticatedClient(shopId: shopId, permissions: ["*"]);
        var uniqueSuffix   = Guid.NewGuid().ToString("N")[..8];
        var inviteEmail    = $"audit-invite-{uniqueSuffix}@integration.test";

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/users/invite",
            new { DisplayName = $"Audit Invite {uniqueSuffix}", Email = inviteEmail });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body      = await response.Content.ReadFromJsonAsync<JsonElement>();
        var invUserId = body.GetInt64();

        // Assert — user row is inactive until AcceptInvite
        await using var scope = fixture.CreateScope();
        var db   = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var user = await db.Users.FindAsync(invUserId);
        user.Should().NotBeNull();
        user!.IsActive.Should().BeFalse(
            "invited users must be inactive until they accept the invite");

        // Assert — an invite security token row exists
        var hasToken = await db.UserSecurityTokens
            .AnyAsync(t => t.UserId == invUserId && t.ConsumedAtUtc == null);
        hasToken.Should().BeTrue(
            "InviteUser must create an invite token row in UserSecurityToken");
    }

    [Fact]
    public async Task Logout_ConsumesRefreshToken()
    {
        // Arrange — login to get tokens
        var (_, email, password) = await fixture.SeedTestUserAsync();
        var unauthClient = fixture.CreateUnauthenticatedClient();

        var loginResp = await unauthClient.PostAsJsonAsync("/api/auth/login",
            new { Identifier = email, Password = password });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody    = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken  = loginBody.GetProperty("accessToken").GetString()!;
        var refreshToken = loginBody.GetProperty("refreshToken").GetString()!;

        // Logout
        var authClient = fixture.CreateUnauthenticatedClient();
        authClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var logoutResp = await authClient.PostAsJsonAsync("/api/auth/logout",
            new { RefreshToken = refreshToken });
        logoutResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert — the token row must now be consumed
        await using var scope = fixture.CreateScope();
        var db   = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        user.Should().NotBeNull();

        // All tokens for this user should have ConsumedAtUtc set (token was consumed by logout)
        var unconsumed = await db.UserSecurityTokens
            .CountAsync(t => t.UserId == user!.Id && t.ConsumedAtUtc == null);
        unconsumed.Should().Be(0,
            "Logout must mark the refresh token as consumed");
    }

    [Fact]
    public async Task CreateRole_PersistsRoleWithCorrectShopId()
    {
        // Arrange
        var (shopId, _, _) = await fixture.SeedTestUserAsync();
        var client         = fixture.CreateAuthenticatedClient(shopId: shopId, permissions: ["*"]);
        var uniqueSuffix   = Guid.NewGuid().ToString("N")[..8];
        var roleCode       = $"AUDROLE_{uniqueSuffix}";

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/roles",
            new { Code = roleCode, Label = $"Audit Role {uniqueSuffix}" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body   = await response.Content.ReadFromJsonAsync<JsonElement>();
        var roleId = body.GetInt64();

        // Assert — role persisted with correct shopId and non-system flag
        await using var scope = fixture.CreateScope();
        var db   = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var role = await db.Roles.FindAsync(roleId);
        role.Should().NotBeNull();
        role!.ShopId.Should().Be(shopId,
            "custom roles must be scoped to the creating shop");
        role.IsSystemRole.Should().BeFalse(
            "user-created roles are never system roles");
    }
}
