using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Metering;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Dapper;
using ErpSaas.Infrastructure.Metering;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Metering;

// ── Test-local TenantDbContext that includes metering schema ──────────────────

internal sealed class MeteringTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        new MeteringModelConfigurator().Configure(modelBuilder);

        // SQLite does not support rowversion
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var rv = entityType.FindProperty("RowVersion");
            if (rv is not null) { rv.IsConcurrencyToken = false; rv.SetDefaultValueSql("0"); }
        }
    }
}

// ── Test-local PlatformDbContext ──────────────────────────────────────────────

internal sealed class MeteringPlatformDbContext(
    DbContextOptions<PlatformDbContext> options,
    AuditSaveChangesInterceptor auditInterceptor)
    : PlatformDbContext(options, auditInterceptor)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var rv = entityType.FindProperty("RowVersion");
            if (rv is not null) { rv.IsConcurrencyToken = false; rv.SetDefaultValueSql("0"); }
        }
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

[Trait("Category", "Unit")]
public sealed class UsageMeterServiceTests : IDisposable
{
    private readonly TenantDbContext _tenantDb;
    private readonly PlatformDbContext _platformDb;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly IDapperContext _dapper = Substitute.For<IDapperContext>();
    private readonly StubTenantCtx _ctx = new(shopId: 1L);
    private readonly SqliteConnection _tenantSqlite;
    private readonly SqliteConnection _platformSqlite;
    private readonly UsageMeterService _sut;

    private const long ShopId = 1L;

    public UsageMeterServiceTests()
    {
        _tenantSqlite = new SqliteConnection("DataSource=:memory:");
        _tenantSqlite.Open();

        _platformSqlite = new SqliteConnection("DataSource=:memory:");
        _platformSqlite.Open();

        var tenantOpts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_tenantSqlite).Options;
        var platformOpts = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlite(_platformSqlite).Options;

        var auditInterceptor = new AuditSaveChangesInterceptor(_ctx);
        _tenantDb  = new MeteringTenantDbContext(tenantOpts, _ctx, auditInterceptor, new TenantSaveChangesInterceptor(_ctx));
        _platformDb = new MeteringPlatformDbContext(platformOpts, auditInterceptor);

        _tenantDb.Database.EnsureCreated();
        _platformDb.Database.EnsureCreated();

        SeedPlan();

        _sut = new UsageMeterService(_tenantDb, _platformDb, _ctx, _dapper, _errorLogger);
    }

    public void Dispose()
    {
        _tenantDb.Dispose();
        _platformDb.Dispose();
        _tenantSqlite.Dispose();
        _platformSqlite.Dispose();
    }

    // ── CheckQuotaAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CheckQuotaAsync_NoPriorUsage_ReturnsAllow()
    {
        var result = await _sut.CheckQuotaAsync(MeterCodes.Invoices, 1);

        Assert.Equal(QuotaCheckStatus.Allow, result.Status);
    }

    [Fact]
    public async Task CheckQuotaAsync_At80PercentUsage_ReturnsWarn()
    {
        SeedMeter(MeterCodes.Invoices, used: 800, quota: 1000, hardCap: false);

        var result = await _sut.CheckQuotaAsync(MeterCodes.Invoices, 1);

        Assert.Equal(QuotaCheckStatus.Warn, result.Status);
    }

    [Fact]
    public async Task CheckQuotaAsync_HardCapExceeded_ReturnsDeny()
    {
        SeedMeter(MeterCodes.Products, used: 500, quota: 500, hardCap: true);

        var result = await _sut.CheckQuotaAsync(MeterCodes.Products, 1);

        Assert.Equal(QuotaCheckStatus.Deny, result.Status);
        Assert.True(result.IsDenied);
    }

    [Fact]
    public async Task CheckQuotaAsync_SoftCapExceeded_ReturnsWarn()
    {
        SeedMeter(MeterCodes.Invoices, used: 1000, quota: 1000, hardCap: false);

        var result = await _sut.CheckQuotaAsync(MeterCodes.Invoices, 1);

        Assert.Equal(QuotaCheckStatus.Warn, result.Status);
    }

    // ── IncrementAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task IncrementAsync_NoPriorMeter_CreatesMeterAndEvent()
    {
        var result = await _sut.IncrementAsync(MeterCodes.Invoices, 1, "Invoice", 42);

        Assert.True(result.IsSuccess);
        var meter = await _tenantDb.Set<UsageMeter>().FirstOrDefaultAsync(m => m.MeterCode == MeterCodes.Invoices);
        Assert.NotNull(meter);
        Assert.Equal(1, meter!.Used);
        var ev = await _tenantDb.Set<UsageEvent>().FirstOrDefaultAsync(e => e.MeterCode == MeterCodes.Invoices);
        Assert.NotNull(ev);
    }

    [Fact]
    public async Task IncrementAsync_ExistingMeter_IncrementsUsed()
    {
        SeedMeter(MeterCodes.Products, used: 10, quota: 500, hardCap: true);

        await _sut.IncrementAsync(MeterCodes.Products, 1);

        var meter = await _tenantDb.Set<UsageMeter>().FirstAsync(m => m.MeterCode == MeterCodes.Products);
        Assert.Equal(11, meter.Used);
    }

    [Fact]
    public async Task IncrementAsync_ExceedsSoftCap_SetsOverageCount()
    {
        SeedMeter(MeterCodes.Invoices, used: 1000, quota: 1000, hardCap: false);

        var result = await _sut.IncrementAsync(MeterCodes.Invoices, 5);

        Assert.True(result.IsSuccess);
        var meter = await _tenantDb.Set<UsageMeter>().FirstAsync(m => m.MeterCode == MeterCodes.Invoices);
        Assert.Equal(5, meter.OverageCount);
        Assert.Equal(QuotaStatus.OverQuota, result.Value);
    }

    // ── GetForecastAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetForecastAsync_HighUsageDailyRate_WillExceedIsTrue()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var daysElapsed = Math.Max(1, (now - monthStart).TotalDays);
        var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);

        // Set used so daily rate projects to exceed quota
        var projectedRate = (long)Math.Ceiling(900.0 / daysElapsed * daysInMonth);
        SeedMeter(MeterCodes.Invoices, used: 900, quota: 1000, hardCap: false, monthStart: monthStart);

        var forecasts = await _sut.GetForecastAsync();
        var invoice = forecasts.FirstOrDefault(f => f.MeterCode == MeterCodes.Invoices);

        Assert.NotNull(invoice);
        if (projectedRate > 1000) Assert.True(invoice!.WillExceed);
    }

    // ── GetHistoryAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistoryAsync_MultiplePeriods_ReturnsPastMonths()
    {
        var now = DateTime.UtcNow;
        var m1 = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-2);
        var m2 = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);
        SeedMeter(MeterCodes.Invoices, used: 100, quota: 1000, hardCap: false, monthStart: m1);
        SeedMeter(MeterCodes.Invoices, used: 200, quota: 1000, hardCap: false, monthStart: m2);

        var history = await _sut.GetHistoryAsync(MeterCodes.Invoices, 6);

        Assert.Equal(2, history.Count);
        Assert.All(history, h => Assert.Equal(MeterCodes.Invoices, h.MeterCode));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SeedPlan()
    {
        _platformDb.Shops.Add(new Shop
        {
            ShopCode = "TEST001", LegalName = "Test Shop",
            CurrencyCode = "INR", TimeZone = "Asia/Kolkata",
            IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        });
        var plan = new SubscriptionPlan
        {
            Code = "Growth", Label = "Growth", MonthlyPrice = 999, AnnualPrice = 9999,
            MaxUsers = 10, MaxProducts = 500, MaxInvoicesPerMonth = 1000,
            SmsQuotaPerMonth = 100, EmailQuotaPerMonth = 500, StorageQuotaMb = 500,
            IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        };
        _platformDb.SubscriptionPlans.Add(plan);
        _platformDb.SaveChanges();
        _platformDb.ShopSubscriptions.Add(new ShopSubscription
        {
            ShopId = ShopId, PlanId = plan.Id, BillingCycle = BillingCycle.Monthly,
            StartsAtUtc = DateTime.UtcNow.AddMonths(-1), IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        });
        _platformDb.SaveChanges();
    }

    private void SeedMeter(string meterCode, long used, long quota, bool hardCap,
        DateTime? monthStart = null)
    {
        var now = DateTime.UtcNow;
        var start = monthStart ?? (MeterCodes.IsMonthly(meterCode)
            ? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            : new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var end = MeterCodes.IsMonthly(meterCode)
            ? start.AddMonths(1).AddTicks(-1)
            : new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        _tenantDb.Set<UsageMeter>().Add(new UsageMeter
        {
            ShopId = ShopId, MeterCode = meterCode,
            PeriodStartUtc = start, PeriodEndUtc = end,
            Used = used, Quota = quota, HardCapEnforced = hardCap,
            OverageCount = 0, OverageChargeRate = 0m,
            CreatedAtUtc = now,
        });
        _tenantDb.SaveChanges();
    }

    private sealed class StubTenantCtx(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
