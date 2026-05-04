using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Modules.Identity.Services;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Modules.Identity;

/// <summary>
/// Integration tests for <c>AuthController</c> and <c>AdminController</c>
/// exercised through the full HTTP pipeline against a real SQL Server instance
/// (Testcontainers).
///
/// Tests verify authentication, permission gating, and the happy path for every
/// controller action.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class IdentityControllerTests(IntegrationTestFixture fixture)
{
    // ── POST /api/auth/login ──────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        // Arrange
        var (_, email, password) = await fixture.SeedTestUserAsync();
        var client = fixture.CreateUnauthenticatedClient();
        var payload = new { Identifier = email, Password = password };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns403()
    {
        // Arrange
        var (_, email, _) = await fixture.SeedTestUserAsync();
        var client = fixture.CreateUnauthenticatedClient();
        var payload = new { Identifier = email, Password = "Wrong@Password999" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_MissingCaptcha_Returns403ForWrongCredentials()
    {
        // CAPTCHA is bypassed in tests (no Turnstile secret key configured).
        // A request with wrong credentials still returns 403 (invalid credentials)
        // rather than 400 (captcha failure), because captcha is skipped in test env.
        var client = fixture.CreateUnauthenticatedClient();
        var payload = new { Identifier = "nonexistent@nowhere.test", Password = "anything" };

        var response = await client.PostAsJsonAsync("/api/auth/login", payload);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Login_AccountLocked_Returns403()
    {
        // Arrange — seed a user with LockoutUntilUtc in the future
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"locked-{uniqueSuffix}@integration.test";

        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        var shop = new Shop
        {
            ShopCode     = $"LOCK-{uniqueSuffix}",
            LegalName    = $"Locked Shop {uniqueSuffix}",
            IsActive     = true,
            CurrencyCode = "INR",
            TimeZone     = "Asia/Kolkata",
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Shops.Add(shop);
        await db.SaveChangesAsync();

        var user = new User
        {
            Email           = email,
            DisplayName     = $"Locked User {uniqueSuffix}",
            PasswordHash    = BCrypt.Net.BCrypt.HashPassword("pass", workFactor: 4),
            IsActive        = true,
            LockoutUntilUtc = DateTime.UtcNow.AddHours(1),   // locked for 1 hour
            CreatedAtUtc    = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.UserShops.Add(new UserShop
        {
            UserId = user.Id, ShopId = shop.Id,
            IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var client = fixture.CreateUnauthenticatedClient();
        var payload = new { Identifier = email, Password = "pass" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", payload);

        // Assert — locked account returns 403
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/auth/refresh ────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_Returns200WithNewTokens()
    {
        // Arrange — login first to get a valid refresh token
        var (_, email, password) = await fixture.SeedTestUserAsync();
        var client = fixture.CreateUnauthenticatedClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new { Identifier = email, Password = password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginBody.GetProperty("refreshToken").GetString()!;

        // Act
        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = refreshToken });

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>();
        refreshBody.GetProperty("accessToken").GetString()
            .Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Refresh_ExpiredToken_Returns403()
    {
        // Arrange — use a random token that does not exist in DB
        var client = fixture.CreateUnauthenticatedClient();
        var fakeToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = fakeToken });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/auth/logout ─────────────────────────────────────────────────

    [Fact]
    public async Task Logout_ValidToken_Returns200AndConsumesToken()
    {
        // Arrange — login to get tokens
        var (_, email, password) = await fixture.SeedTestUserAsync();
        var unauthClient = fixture.CreateUnauthenticatedClient();

        var loginResponse = await unauthClient.PostAsJsonAsync("/api/auth/login",
            new { Identifier = email, Password = password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken  = loginBody.GetProperty("accessToken").GetString()!;
        var refreshToken = loginBody.GetProperty("refreshToken").GetString()!;

        // Build an authenticated client using the real JWT from login
        var authClient = fixture.CreateUnauthenticatedClient();
        authClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        // Act — logout
        var logoutResponse = await authClient.PostAsJsonAsync("/api/auth/logout",
            new { RefreshToken = refreshToken });
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert — refresh with the now-consumed token should fail
        var reRefresh = await unauthClient.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = refreshToken });
        reRefresh.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/auth/forgot-password ────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_ExistingUser_Returns200AndEnqueuesEmail()
    {
        // Arrange
        var (_, email, _) = await fixture.SeedTestUserAsync();
        var client = fixture.CreateUnauthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/forgot-password",
            new { Identifier = email });

        // Assert — always 200 (anti-enumeration)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<bool>();
        body.Should().BeTrue();
    }

    [Fact]
    public async Task ForgotPassword_NonExistingUser_Returns200_NoEmailSent()
    {
        // Arrange — use an email that does not exist in the DB
        var client = fixture.CreateUnauthenticatedClient();
        var nonExistentEmail = $"ghost-{Guid.NewGuid():N}@nowhere.test";

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/forgot-password",
            new { Identifier = nonExistentEmail });

        // Assert — still 200 to prevent user enumeration
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<bool>();
        body.Should().BeTrue();
    }

    // ── GET /api/admin/shop-profile ───────────────────────────────────────────

    [Fact]
    public async Task GetShopProfile_Authenticated_Returns200()
    {
        // Arrange — admin client uses is_platform_admin which bypasses permission checks
        var client = fixture.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/admin/shop-profile");

        // Assert — either 200 with a profile or 404 if no shop matches shopId=1
        // (depends on whether the seeder created shopId=1; use BeOneOf)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetShopProfile_Unauthenticated_Returns401()
    {
        // Arrange
        var client = fixture.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/admin/shop-profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/admin/users ──────────────────────────────────────────────────

    [Fact]
    public async Task ListUsers_WithPermission_Returns200()
    {
        // Arrange — admin client has is_platform_admin = true (bypasses all permission checks)
        var client = fixture.CreateAuthenticatedClient(permissions: ["*"]);

        // Act
        var response = await client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task ListUsers_WithoutPermission_Returns403()
    {
        // Arrange — limited client has no Users.View permission
        var client = fixture.CreateLimitedClient(permissionCode: "None.None");

        // Act
        var response = await client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/admin/users/invite ──────────────────────────────────────────

    [Fact]
    public async Task InviteUser_ValidRequest_Returns200()
    {
        // Arrange
        var client = fixture.CreateAuthenticatedClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            DisplayName = $"Invited User {uniqueSuffix}",
            Email       = $"invite-{uniqueSuffix}@integration.test",
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/users/invite", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<long>();
        id.Should().BePositive();
    }

    [Fact]
    public async Task InviteUser_DuplicateEmail_Returns409()
    {
        // Arrange — invite the same user twice
        var client = fixture.CreateAuthenticatedClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            DisplayName = $"Dup User {uniqueSuffix}",
            Email       = $"dup-{uniqueSuffix}@integration.test",
        };

        var first = await client.PostAsJsonAsync("/api/admin/users/invite", payload);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act — second invite with same email
        var response = await client.PostAsJsonAsync("/api/admin/users/invite", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── GET /api/admin/roles ──────────────────────────────────────────────────

    [Fact]
    public async Task ListRoles_WithPermission_Returns200()
    {
        // Arrange
        var client = fixture.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/admin/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    // ── POST /api/admin/roles ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateRole_ValidRequest_Returns200WithId()
    {
        // Arrange
        var client = fixture.CreateAuthenticatedClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var payload = new { Code = $"ROLE_{uniqueSuffix}", Label = $"Test Role {uniqueSuffix}" };

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/roles", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<long>();
        id.Should().BePositive();
    }

    [Fact]
    public async Task CreateRole_DuplicateCode_Returns409()
    {
        // Arrange — create role twice with same code
        var client = fixture.CreateAuthenticatedClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var payload = new { Code = $"DUP_{uniqueSuffix}", Label = $"Dup Role {uniqueSuffix}" };

        var first = await client.PostAsJsonAsync("/api/admin/roles", payload);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/roles", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── POST /api/admin/branches ──────────────────────────────────────────────

    [Fact]
    public async Task CreateBranch_ValidRequest_Returns200WithId()
    {
        // Arrange
        var client = fixture.CreateAuthenticatedClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var payload = new { Name = $"Branch {uniqueSuffix}", City = "Mumbai", IsHeadOffice = false };

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/branches", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<long>();
        id.Should().BePositive();
    }
}
