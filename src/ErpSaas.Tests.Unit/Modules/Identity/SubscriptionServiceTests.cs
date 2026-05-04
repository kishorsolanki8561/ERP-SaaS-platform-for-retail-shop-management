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
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Identity;

[Trait("Category", "Unit")]
public class SubscriptionServiceTests : IDisposable
{
    private readonly PlatformDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly SubscriptionService _sut;
    private readonly SqliteConnection _sqliteConnection;

    private const long ShopId = 1L;

    public SubscriptionServiceTests()
    {
        var stubCtx = new StubTenantContext(shopId: ShopId);

        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var opts = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        _db = new IdentityPlatformDbContext(opts, new AuditSaveChangesInterceptor(stubCtx));
        _db.Database.EnsureCreated();

        SeedShopAndPlans();

        _sut = new SubscriptionService(_db, _errorLogger, stubCtx, Substitute.For<ILogger<SubscriptionService>>());
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqliteConnection.Dispose();
    }

    // ── ListPlansAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListPlansAsync_ActivePlansExist_ReturnsAllActive()
    {
        var plans = await _sut.ListPlansAsync();

        plans.Should().HaveCount(2);
        plans.Select(p => p.Code).Should().Contain(["Starter", "Growth"]);
    }

    [Fact]
    public async Task ListPlansAsync_InactivePlanExists_ExcludesInactive()
    {
        _db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Code = "Hidden", Label = "Hidden Plan", MonthlyPrice = 9999, AnnualPrice = 99999,
            IsActive = false, CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var plans = await _sut.ListPlansAsync();

        plans.Select(p => p.Code).Should().NotContain("Hidden");
    }

    [Fact]
    public async Task ListPlansAsync_ReturnsPlansOrderedByMonthlyPrice()
    {
        var plans = await _sut.ListPlansAsync();

        plans.Select(p => p.MonthlyPrice).Should()
            .BeInAscendingOrder();
    }

    // ── GetCurrentAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentAsync_NoSubscription_ReturnsNull()
    {
        var result = await _sut.GetCurrentAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentAsync_ActiveSubscriptionExists_ReturnsCurrent()
    {
        var plan = await _db.SubscriptionPlans.FirstAsync(p => p.Code == "Growth");
        _db.ShopSubscriptions.Add(MakeSubscription(plan.Id, BillingCycle.Monthly));
        await _db.SaveChangesAsync();

        var result = await _sut.GetCurrentAsync();

        result.Should().NotBeNull();
        result!.PlanCode.Should().Be("Growth");
        result.BillingCycle.Should().Be("Monthly");
        result.IsActive.Should().BeTrue();
    }

    // ── ChangePlanAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePlanAsync_ValidPlan_ReturnsSuccess()
    {
        var dto = new ChangePlanDto("Growth", "Monthly");

        var result = await _sut.ChangePlanAsync(dto);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePlanAsync_ValidPlan_CreatesNewSubscription()
    {
        var dto = new ChangePlanDto("Growth", "Annual");

        await _sut.ChangePlanAsync(dto);

        var sub = await _db.ShopSubscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.ShopId == ShopId && s.IsActive);

        sub.Should().NotBeNull();
        sub!.Plan.Code.Should().Be("Growth");
        sub.BillingCycle.Should().Be(BillingCycle.Annual);
    }

    [Fact]
    public async Task ChangePlanAsync_ExistingActiveSub_DeactivatesOld()
    {
        var plan = await _db.SubscriptionPlans.FirstAsync(p => p.Code == "Starter");
        _db.ShopSubscriptions.Add(MakeSubscription(plan.Id, BillingCycle.Monthly));
        await _db.SaveChangesAsync();

        var dto = new ChangePlanDto("Growth", "Monthly");
        await _sut.ChangePlanAsync(dto);

        var inactiveSub = await _db.ShopSubscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Plan.Code == "Starter");

        inactiveSub!.IsActive.Should().BeFalse();
        inactiveSub.EndsAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangePlanAsync_SamePlanAndCycle_ReturnsConflict()
    {
        var plan = await _db.SubscriptionPlans.FirstAsync(p => p.Code == "Growth");
        _db.ShopSubscriptions.Add(MakeSubscription(plan.Id, BillingCycle.Monthly));
        await _db.SaveChangesAsync();

        var dto = new ChangePlanDto("Growth", "Monthly");
        var result = await _sut.ChangePlanAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Should().Be(Errors.Subscription.AlreadyOnPlan);
    }

    [Fact]
    public async Task ChangePlanAsync_UnknownPlanCode_ReturnsNotFound()
    {
        var dto = new ChangePlanDto("NonExistent", "Monthly");

        var result = await _sut.ChangePlanAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Should().Be(Errors.Subscription.PlanNotFound);
    }

    [Fact]
    public async Task ChangePlanAsync_InvalidBillingCycle_ReturnsFailure()
    {
        var dto = new ChangePlanDto("Growth", "Quarterly");

        var result = await _sut.ChangePlanAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Should().Be(Errors.Subscription.InvalidBillingCycle);
    }

    // ── CancelAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_NoActiveSubscription_ReturnsNotFound()
    {
        var result = await _sut.CancelAsync();

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Should().Be(Errors.Subscription.NoActiveSubscription);
    }

    [Fact]
    public async Task CancelAsync_StarterPlan_ReturnsConflict()
    {
        var plan = await _db.SubscriptionPlans.FirstAsync(p => p.Code == "Starter");
        _db.ShopSubscriptions.Add(MakeSubscription(plan.Id, BillingCycle.Monthly));
        await _db.SaveChangesAsync();

        var result = await _sut.CancelAsync();

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Should().Be(Errors.Subscription.CannotCancelFree);
    }

    [Fact]
    public async Task CancelAsync_PaidPlan_DeactivatesSubscription()
    {
        var plan = await _db.SubscriptionPlans.FirstAsync(p => p.Code == "Growth");
        _db.ShopSubscriptions.Add(MakeSubscription(plan.Id, BillingCycle.Annual));
        await _db.SaveChangesAsync();

        var result = await _sut.CancelAsync();

        result.IsSuccess.Should().BeTrue();

        var sub = await _db.ShopSubscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Plan.Code == "Growth");
        sub!.IsActive.Should().BeFalse();
        sub.EndsAtUtc.Should().NotBeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SeedShopAndPlans()
    {
        _db.Shops.Add(new Shop
        {
            ShopCode = "TST001", LegalName = "Test Shop", CurrencyCode = "INR",
            TimeZone = "Asia/Kolkata", IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        });
        _db.SubscriptionPlans.AddRange(
            new SubscriptionPlan
            {
                Code = "Starter", Label = "Starter (Free)", MonthlyPrice = 0, AnnualPrice = 0,
                MaxUsers = 2, IsActive = true, CreatedAtUtc = DateTime.UtcNow
            },
            new SubscriptionPlan
            {
                Code = "Growth", Label = "Growth", MonthlyPrice = 999, AnnualPrice = 9990,
                MaxUsers = 10, IsActive = true, CreatedAtUtc = DateTime.UtcNow
            });
        _db.SaveChanges();
    }

    private static ShopSubscription MakeSubscription(long planId, BillingCycle cycle) =>
        new()
        {
            ShopId       = ShopId,
            PlanId       = planId,
            BillingCycle = cycle,
            StartsAtUtc  = DateTime.UtcNow,
            IsActive     = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
