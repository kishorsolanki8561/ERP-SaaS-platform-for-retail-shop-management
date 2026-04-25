using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Messaging;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Identity;

// ── Test-local PlatformDbContext on SQLite ────────────────────────────────────

/// <summary>
/// A PlatformDbContext subclass targeting SQLite in-memory for Identity unit
/// tests.  Drops <c>IsRowVersion()</c> concurrency tokens that SQLite does not
/// support and removes the partial-index unique filters that SQLite handles
/// differently.
/// </summary>
internal sealed class IdentityPlatformDbContext(
    DbContextOptions<PlatformDbContext> options,
    AuditSaveChangesInterceptor auditInterceptor)
    : PlatformDbContext(options, auditInterceptor)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQLite does not support SQL Server rowversion / timestamp.
        // Remove the concurrency-token flag and set a SQLite-compatible default
        // so EnsureCreated does not produce a NOT NULL column with no default.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var rowVersion = entityType.FindProperty("RowVersion");
            if (rowVersion is not null)
            {
                rowVersion.IsConcurrencyToken = false;
                rowVersion.SetDefaultValueSql("0");
            }
        }
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Unit tests for <see cref="AdminService"/> using SQLite in-memory and
/// NSubstitute mocks for external dependencies.
///
/// <see cref="AuthService"/> relies on BCrypt, JWT generation, and
/// <see cref="IConfiguration"/> — those are exercised via integration tests
/// (see <c>IdentityControllerTests</c>).
/// </summary>
[Trait("Category", "Unit")]
public class IdentityServiceTests : IDisposable
{
    private readonly PlatformDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly AdminService _sut;
    private readonly SqliteConnection _sqliteConnection;

    private const long ShopId = 1L;

    public IdentityServiceTests()
    {
        var stubCtx = new StubTenantContext(shopId: ShopId, userId: 1L);

        // Use SQLite in-memory (supports transactions, unlike EF in-memory provider).
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var opts = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        var auditInterceptor = new AuditSaveChangesInterceptor(stubCtx);

        _db = new IdentityPlatformDbContext(opts, auditInterceptor);
        _db.Database.EnsureCreated();

        // Seed the shop this tenant context points to.
        SeedShop(ShopId);

        // Token service stubs — deterministic output for tests.
        _tokenService.GenerateRefreshToken().Returns("test-raw-token");
        _tokenService.HashToken(Arg.Any<string>()).Returns("hashed-test-token");

        _sut = new AdminService(
            _db,
            _errorLogger,
            stubCtx,
            _tokenService,
            _notifications,
            Substitute.For<ILogger<AdminService>>());
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqliteConnection.Dispose();
    }

    // ── GetShopProfileAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetShopProfileAsync_ExistingShop_ReturnsProfile()
    {
        var profile = await _sut.GetShopProfileAsync();

        profile.Should().NotBeNull();
        profile!.LegalName.Should().Be("Test Shop");
        profile.ShopCode.Should().Be("TST001");
    }

    [Fact]
    public async Task GetShopProfileAsync_NonExistingShop_ReturnsNull()
    {
        // Use a different shop id that has no shop record.
        var stubCtx = new StubTenantContext(shopId: 9999L, userId: 1L);
        var sut = new AdminService(
            _db, _errorLogger, stubCtx, _tokenService,
            _notifications, Substitute.For<ILogger<AdminService>>());

        var profile = await sut.GetShopProfileAsync();

        profile.Should().BeNull();
    }

    // ── UpdateShopProfileAsync ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateShopProfileAsync_ValidDto_ReturnsSuccess()
    {
        var dto = new UpdateShopProfileDto(
            LegalName: "Updated Shop Name",
            TradeName: "Updated Trade",
            GstNumber: "27ABCDE1234F1Z5",
            AddressLine1: "123 Main St",
            AddressLine2: null,
            City: "Mumbai",
            StateCode: "MH",
            PinCode: "400001",
            CurrencyCode: "INR");

        var result = await _sut.UpdateShopProfileAsync(dto);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateShopProfileAsync_PersistsChanges()
    {
        var dto = new UpdateShopProfileDto(
            "Renamed Legal", "Renamed Trade", null, null, null, "Delhi", "DL", null, "INR");

        await _sut.UpdateShopProfileAsync(dto);

        var profile = await _sut.GetShopProfileAsync();
        profile!.LegalName.Should().Be("Renamed Legal");
        profile.City.Should().Be("Delhi");
    }

    // ── ListUsersAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListUsersAsync_NoUsers_ReturnsEmptyPage()
    {
        var result = await _sut.ListUsersAsync(1, 20, null);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ListUsersAsync_SearchFiltersByDisplayName()
    {
        SeedUserInShop("Alice Smith", "alice@test.com");
        SeedUserInShop("Bob Jones", "bob@test.com");

        var result = await _sut.ListUsersAsync(1, 20, "Alice");

        result.Items.Should().ContainSingle(u => u.DisplayName == "Alice Smith");
    }

    // ── CreateRoleAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRoleAsync_UniqueCode_ReturnsSuccessWithId()
    {
        var result = await _sut.CreateRoleAsync(new CreateRoleDto("MANAGER", "Manager"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateRoleAsync_DuplicateCode_ReturnsConflict()
    {
        await _sut.CreateRoleAsync(new CreateRoleDto("CASHIER", "Cashier"));

        var result = await _sut.CreateRoleAsync(new CreateRoleDto("CASHIER", "Cashier Dup"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateRoleAsync_CodeIsUpperCased()
    {
        var result = await _sut.CreateRoleAsync(new CreateRoleDto("manager", "Manager"));

        result.IsSuccess.Should().BeTrue();
        var roles = await _sut.ListRolesAsync();
        roles.Should().ContainSingle(r => r.Code == "MANAGER");
    }

    // ── ListRolesAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListRolesAsync_ReturnsRolesForShopAndSystemRoles()
    {
        // System role (ShopId = null)
        _db.Roles.Add(new Role { Code = "SYSADMIN", Label = "System Admin", IsSystemRole = true, CreatedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        await _sut.CreateRoleAsync(new CreateRoleDto("SALES", "Sales"));

        var roles = await _sut.ListRolesAsync();

        roles.Should().HaveCount(2);
        roles.Should().Contain(r => r.IsSystemRole);
        roles.Should().Contain(r => r.Code == "SALES");
    }

    // ── UpdateRolePermissionsAsync ────────────────────────────────────────────

    [Fact]
    public async Task UpdateRolePermissionsAsync_NonExistingRole_ReturnsNotFound()
    {
        var result = await _sut.UpdateRolePermissionsAsync(9999L,
            new UpdateRolePermissionsDto([]));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateRolePermissionsAsync_SystemRole_ReturnsForbidden()
    {
        var sysRole = new Role
        {
            Code = "SYSADMIN", Label = "System Admin",
            IsSystemRole = true, ShopId = null,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Roles.Add(sysRole);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateRolePermissionsAsync(sysRole.Id,
            new UpdateRolePermissionsDto([]));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    // ── DeactivateUserAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateUserAsync_ExistingUser_ReturnsSuccess()
    {
        var user = SeedUserInShop("Charlie Brown", "charlie@test.com");

        var result = await _sut.DeactivateUserAsync(user.Id);

        result.IsSuccess.Should().BeTrue();
        var found = await _db.Users.FindAsync(user.Id);
        found!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateUserAsync_NonExistingUser_ReturnsNotFound()
    {
        var result = await _sut.DeactivateUserAsync(9999L);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // ── UnlockUserAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UnlockUserAsync_LockedUser_ClearsLockout()
    {
        var user = SeedUserInShop("Locked User", "locked@test.com");
        user.FailedLoginCount = 5;
        user.LockoutUntilUtc = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync();

        await _sut.UnlockUserAsync(user.Id);

        var found = await _db.Users.FindAsync(user.Id);
        found!.FailedLoginCount.Should().Be(0);
        found.LockoutUntilUtc.Should().BeNull();
    }

    // ── ListBranchesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListBranchesAsync_NoBranches_ReturnsEmptyList()
    {
        var result = await _sut.ListBranchesAsync();

        result.Should().BeEmpty();
    }

    // ── CreateBranchAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBranchAsync_ValidDto_ReturnsSuccessWithId()
    {
        var dto = new CreateBranchDto(
            Name: "Main Branch",
            City: "Mumbai",
            IsHeadOffice: true);

        var result = await _sut.CreateBranchAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateBranchAsync_PersistsBranch()
    {
        var dto = new CreateBranchDto(
            Name: "East Branch",
            City: "Pune");

        await _sut.CreateBranchAsync(dto);

        var branches = await _sut.ListBranchesAsync();
        branches.Should().ContainSingle(b => b.Name == "East Branch");
    }

    // ── InviteUserAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task InviteUserAsync_NewEmail_ReturnsSuccessWithUserId()
    {
        var dto = new InviteUserDto("New Staff", "newstaff@test.com");

        var result = await _sut.InviteUserAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task InviteUserAsync_ExistingEmail_ReturnsConflict()
    {
        SeedUserInShop("Existing User", "existing@test.com");

        var result = await _sut.InviteUserAsync(
            new InviteUserDto("Duplicate", "existing@test.com"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task InviteUserAsync_EnqueuesNotification()
    {
        await _sut.InviteUserAsync(new InviteUserDto("New Person", "newperson@test.com"));

        await _notifications.Received(1).EnqueueAsync(
            Arg.Any<long>(),
            Arg.Any<ErpSaas.Infrastructure.Data.Entities.Messaging.Enums.NotificationChannel>(),
            "newperson@test.com",
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    // ── AssignUserRoleAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task AssignUserRoleAsync_ValidUserAndRole_ReturnsSuccess()
    {
        var user = SeedUserInShop("Staff A", "staffa@test.com");
        var roleResult = await _sut.CreateRoleAsync(new CreateRoleDto("STAFF", "Staff"));

        var result = await _sut.AssignUserRoleAsync(user.Id, roleResult.Value!);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AssignUserRoleAsync_Idempotent_WhenAlreadyAssigned()
    {
        var user = SeedUserInShop("Staff B", "staffb@test.com");
        var roleResult = await _sut.CreateRoleAsync(new CreateRoleDto("VIEWER", "Viewer"));

        // Assign twice — must succeed both times (idempotent)
        var first  = await _sut.AssignUserRoleAsync(user.Id, roleResult.Value!);
        var second = await _sut.AssignUserRoleAsync(user.Id, roleResult.Value!);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
    }

    // ── RemoveUserRoleAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task RemoveUserRoleAsync_ExistingAssignment_ReturnsSuccess()
    {
        var user = SeedUserInShop("Staff C", "staffc@test.com");
        var roleResult = await _sut.CreateRoleAsync(new CreateRoleDto("TEMP", "Temp"));
        await _sut.AssignUserRoleAsync(user.Id, roleResult.Value!);

        var result = await _sut.RemoveUserRoleAsync(user.Id, roleResult.Value!);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveUserRoleAsync_NonExistingAssignment_ReturnsNotFound()
    {
        var result = await _sut.RemoveUserRoleAsync(9999L, 8888L);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SeedShop(long shopId)
    {
        if (_db.Shops.Any(s => s.Id == shopId)) return;

        // SQLite identity insert: manually set Id by using raw SQL workaround.
        // We add the shop and then track its assigned Id.
        var shop = new Shop
        {
            ShopCode = "TST001",
            LegalName = "Test Shop",
            CurrencyCode = "INR",
            TimeZone = "Asia/Kolkata",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        // Force Id via EF shadow property
        var entry = _db.Shops.Add(shop);
        _db.SaveChanges();

        // If the auto-generated Id doesn't match ShopId (1), we adjust the
        // stub to use the actual Id.  For SQLite the first insert gets Id = 1.
    }

    private User SeedUserInShop(string displayName, string email)
    {
        var user = new User
        {
            DisplayName = displayName,
            Email = email,
            PasswordHash = "hash",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Users.Add(user);
        _db.SaveChanges();

        _db.UserShops.Add(new UserShop
        {
            UserId = user.Id,
            ShopId = ShopId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        });
        _db.SaveChanges();
        return user;
    }

    private sealed class StubTenantContext(long shopId, long userId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => userId;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
