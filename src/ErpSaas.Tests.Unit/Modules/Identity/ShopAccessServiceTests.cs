using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Identity;

[Trait("Category", "Unit")]
public class ShopAccessServiceTests : IDisposable
{
    private readonly PlatformDbContext _db;
    private readonly IPermissionService _permissionService = Substitute.For<IPermissionService>();
    private readonly StubAccessTenantContext _tenant;
    private readonly ShopAccessService _sut;
    private readonly SqliteConnection _conn;

    private const long ShopId     = 1L;
    private const long AdminUserId = 10L;
    private const long TargetUserId = 20L;

    public ShopAccessServiceTests()
    {
        _tenant = new StubAccessTenantContext(ShopId, AdminUserId);

        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opts = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlite(_conn)
            .Options;

        _db = new IdentityPlatformDbContext(opts, new AuditSaveChangesInterceptor(_tenant));
        _db.Database.EnsureCreated();

        SeedBaseData();

        _sut = new ShopAccessService(_db, _permissionService, _tenant,
            Substitute.For<IErrorLogger>());
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    // ── SetModuleVisibilityAsync ───────────────────────────────────────────────

    [Fact]
    public async Task SetModuleVisibility_WhenFeatureInPlan_WritesDisableOverride()
    {
        var result = await _sut.SetModuleVisibilityAsync(
            new SetModuleVisibilityDto("Module.Billing", false));

        result.IsSuccess.Should().BeTrue();
        var ov = await _db.ShopFeatureOverrides.FirstOrDefaultAsync(
            o => o.ShopId == ShopId && o.FeatureCode == "Module.Billing");
        ov.Should().NotBeNull();
        ov!.IsEnabled.Should().BeFalse();
        _permissionService.Received(1).InvalidateShopFeatureCache(ShopId);
    }

    [Fact]
    public async Task SetModuleVisibility_WhenFeatureInPlan_WritesEnableOverride()
    {
        var result = await _sut.SetModuleVisibilityAsync(
            new SetModuleVisibilityDto("Module.Billing", true));

        result.IsSuccess.Should().BeTrue();
        var ov = await _db.ShopFeatureOverrides.FirstOrDefaultAsync(
            o => o.ShopId == ShopId && o.FeatureCode == "Module.Billing");
        ov.Should().NotBeNull();
        ov!.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task SetModuleVisibility_WhenFeatureNotInPlan_AndEnable_ReturnsFailure()
    {
        var result = await _sut.SetModuleVisibilityAsync(
            new SetModuleVisibilityDto("Module.HR", true));

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Should().Be(Errors.ShopAccess.FeatureNotInPlan);
    }

    [Fact]
    public async Task SetModuleVisibility_WhenFeatureNotInPlan_AndDisable_ReturnsFailure()
    {
        var result = await _sut.SetModuleVisibilityAsync(
            new SetModuleVisibilityDto("Module.HR", false));

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Should().Be(Errors.ShopAccess.FeatureNotInPlan);
    }

    [Fact]
    public async Task SetModuleVisibility_NonModuleCode_ReturnsFailure()
    {
        var result = await _sut.SetModuleVisibilityAsync(
            new SetModuleVisibilityDto("Billing.BarcodePos", true));

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Should().Be(Errors.ShopAccess.FeatureNotModuleLevel);
    }

    [Fact]
    public async Task SetModuleVisibility_ExistingOverride_UpdatesInPlace()
    {
        _db.ShopFeatureOverrides.Add(new ShopFeatureOverride
        {
            ShopId = ShopId, FeatureCode = "Module.Billing",
            IsEnabled = true, CreatedAtUtc = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        var result = await _sut.SetModuleVisibilityAsync(
            new SetModuleVisibilityDto("Module.Billing", false));

        result.IsSuccess.Should().BeTrue();
        var count = await _db.ShopFeatureOverrides.CountAsync(
            o => o.ShopId == ShopId && o.FeatureCode == "Module.Billing");
        count.Should().Be(1); // no duplicate row
        var ov = await _db.ShopFeatureOverrides.FirstAsync(
            o => o.ShopId == ShopId && o.FeatureCode == "Module.Billing");
        ov.IsEnabled.Should().BeFalse();
    }

    // ── SetUserPermissionOverrideAsync ────────────────────────────────────────

    [Fact]
    public async Task SetUserPermissionOverride_WhenModuleInPlan_WritesOverride()
    {
        _permissionService.GetFeatureCodesAsync(ShopId, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "Module.Billing" });

        var result = await _sut.SetUserPermissionOverrideAsync(TargetUserId,
            new SetPermissionOverrideDto("Billing.View", true));

        result.IsSuccess.Should().BeTrue();
        var ov = await _db.UserPermissionOverrides.FirstOrDefaultAsync(
            o => o.UserId == TargetUserId && o.ShopId == ShopId && o.PermissionCode == "Billing.View");
        ov.Should().NotBeNull();
        ov!.IsGranted.Should().BeTrue();
        _permissionService.Received(1).InvalidateUserPermissionCache(TargetUserId, ShopId);
    }

    [Fact]
    public async Task SetUserPermissionOverride_WhenModuleNotInPlan_AndGrant_ReturnsFailure()
    {
        _permissionService.GetFeatureCodesAsync(ShopId, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "Module.Billing" });

        var result = await _sut.SetUserPermissionOverrideAsync(TargetUserId,
            new SetPermissionOverrideDto("Marketplace.Sync", true));

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Should().Be(Errors.ShopAccess.PermissionModuleNotInPlan);
    }

    [Fact]
    public async Task SetUserPermissionOverride_WhenUserNotInShop_ReturnsNotFound()
    {
        const long unknownUserId = 999L;

        var result = await _sut.SetUserPermissionOverrideAsync(unknownUserId,
            new SetPermissionOverrideDto("Billing.View", false));

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Should().Be(Errors.ShopAccess.UserNotInShop);
    }

    // ── RemoveUserPermissionOverrideAsync ─────────────────────────────────────

    [Fact]
    public async Task RemoveUserPermissionOverride_WhenExists_Removes()
    {
        _db.UserPermissionOverrides.Add(new UserPermissionOverride
        {
            UserId = TargetUserId, ShopId = ShopId,
            PermissionCode = "Billing.View", IsGranted = true,
            SetByUserId = AdminUserId, CreatedAtUtc = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        var result = await _sut.RemoveUserPermissionOverrideAsync(TargetUserId, "Billing.View");

        result.IsSuccess.Should().BeTrue();
        var count = await _db.UserPermissionOverrides.CountAsync(
            o => o.UserId == TargetUserId && o.PermissionCode == "Billing.View");
        count.Should().Be(0);
    }

    [Fact]
    public async Task RemoveUserPermissionOverride_WhenNotExists_ReturnsNotFound()
    {
        var result = await _sut.RemoveUserPermissionOverrideAsync(TargetUserId, "Billing.View");

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Should().Be(Errors.ShopAccess.OverrideNotFound);
    }

    // ── GetModuleAccessAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetModuleAccess_ReturnsCorrectEffectiveStatus_WithAndWithoutOverrides()
    {
        // Module.Billing is in plan; add a disable override for it
        _db.ShopFeatureOverrides.Add(new ShopFeatureOverride
        {
            ShopId = ShopId, FeatureCode = "Module.Billing",
            IsEnabled = false, CreatedAtUtc = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        _permissionService.GetFeatureCodesAsync(ShopId, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "Module.Dashboard", "Module.Inventory" });

        var modules = await _sut.GetModuleAccessAsync();

        modules.Should().NotBeEmpty();
        var billing = modules.First(m => m.FeatureCode == "Module.Billing");
        billing.IsInPlan.Should().BeTrue();
        billing.HasOverride.Should().BeTrue();
        billing.OverrideValue.Should().BeFalse();

        var hr = modules.First(m => m.FeatureCode == "Module.HR");
        hr.IsInPlan.Should().BeFalse();
        hr.HasOverride.Should().BeFalse();
    }

    // ── GetUserPermissionsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetUserPermissions_ReturnsBaseRolePlusOverrides()
    {
        // Seed a permission, role, and override
        var perm = _db.Permissions.First();
        var role = _db.Roles.First();
        _db.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id, PermissionId = perm.Id, CreatedAtUtc = DateTime.UtcNow
        });
        _db.UserRoles.Add(new UserRole
        {
            UserId = TargetUserId, ShopId = ShopId, RoleId = role.Id, CreatedAtUtc = DateTime.UtcNow
        });
        _db.UserPermissionOverrides.Add(new UserPermissionOverride
        {
            UserId = TargetUserId, ShopId = ShopId,
            PermissionCode = perm.Code, IsGranted = false,
            SetByUserId = AdminUserId, CreatedAtUtc = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        var summary = await _sut.GetUserPermissionsAsync(TargetUserId);

        summary.Should().NotBeNull();
        summary!.UserId.Should().Be(TargetUserId);
        var permStatus = summary.Permissions.First(p => p.Code == perm.Code);
        permStatus.IsFromRole.Should().BeTrue();
        permStatus.HasOverride.Should().BeTrue();
        permStatus.IsGranted.Should().BeFalse(); // revoked by override
    }

    [Fact]
    public async Task GetUserPermissions_UserNotInShop_ReturnsNull()
    {
        var summary = await _sut.GetUserPermissionsAsync(999L);

        summary.Should().BeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SeedBaseData()
    {
        var shop = new Shop
        {
            ShopCode = "TST001", LegalName = "Test Shop", CurrencyCode = "INR",
            TimeZone = "Asia/Kolkata", IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Shops.Add(shop);
        _db.SaveChanges();

        var plan = new SubscriptionPlan
        {
            Code = "Starter", Label = "Starter", MonthlyPrice = 0, AnnualPrice = 0,
            MaxUsers = 5, IsActive = true, CreatedAtUtc = DateTime.UtcNow
        };
        _db.SubscriptionPlans.Add(plan);
        _db.SaveChanges();

        _db.SubscriptionPlanFeatures.AddRange(
            new SubscriptionPlanFeature { PlanId = plan.Id, FeatureCode = "Module.Billing",   CreatedAtUtc = DateTime.UtcNow },
            new SubscriptionPlanFeature { PlanId = plan.Id, FeatureCode = "Module.Dashboard", CreatedAtUtc = DateTime.UtcNow },
            new SubscriptionPlanFeature { PlanId = plan.Id, FeatureCode = "Module.Inventory", CreatedAtUtc = DateTime.UtcNow }
        );

        _db.ShopSubscriptions.Add(new ShopSubscription
        {
            ShopId = ShopId, PlanId = plan.Id,
            BillingCycle = BillingCycle.Monthly,
            StartsAtUtc = DateTime.UtcNow, IsActive = true, CreatedAtUtc = DateTime.UtcNow
        });

        // Target user + membership
        var targetUser = new User
        {
            Id = TargetUserId, DisplayName = "Staff Member", Email = "staff@test.com",
            PasswordHash = "hash", IsActive = true, CreatedAtUtc = DateTime.UtcNow
        };
        _db.Users.Add(targetUser);
        _db.UserShops.Add(new UserShop
        {
            UserId = TargetUserId, ShopId = ShopId, IsActive = true, CreatedAtUtc = DateTime.UtcNow
        });

        // A permission and role for override tests
        var permission = new Permission
        {
            Code = "Billing.View", Label = "View Billing", Module = "Billing", CreatedAtUtc = DateTime.UtcNow
        };
        _db.Permissions.Add(permission);

        var role = new Role
        {
            ShopId = ShopId, Code = "Staff", Label = "Staff",
            IsSystemRole = false, CreatedAtUtc = DateTime.UtcNow
        };
        _db.Roles.Add(role);

        _db.SaveChanges();
    }

    private sealed class StubAccessTenantContext(long shopId, long userId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => userId;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
