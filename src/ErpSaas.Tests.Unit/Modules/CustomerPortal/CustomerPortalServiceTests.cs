using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Portal;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.CustomerPortal.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.CustomerPortal;

[Trait("Category", "Unit")]
public class CustomerPortalServiceTests : IDisposable
{
    private readonly PlatformDbContext _platformDb;
    private readonly TenantDbContext _tenantDb;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly CustomerPortalService _sut;
    private readonly SqliteConnection _platformConn;
    private readonly SqliteConnection _tenantConn;

    private const long PlatformCustomerId = 1L;
    private const long ShopId = 10L;

    public CustomerPortalServiceTests()
    {
        var stubCtx = new StubTenantContext(shopId: ShopId);

        _platformConn = new SqliteConnection("DataSource=:memory:");
        _platformConn.Open();
        var platformOpts = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlite(_platformConn).Options;
        _platformDb = new PortalPlatformDbContext(platformOpts, new AuditSaveChangesInterceptor(stubCtx));
        _platformDb.Database.EnsureCreated();

        _tenantConn = new SqliteConnection("DataSource=:memory:");
        _tenantConn.Open();
        var tenantOpts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_tenantConn).Options;
        _tenantDb = new PortalTenantDbContext(tenantOpts, stubCtx, new AuditSaveChangesInterceptor(stubCtx), new TenantSaveChangesInterceptor(stubCtx), []);
        _tenantDb.Database.EnsureCreated();

        SeedCustomer();

        _sut = new CustomerPortalService(
            _platformDb,
            _tenantDb,
            _errorLogger,
            Substitute.For<ILogger<CustomerPortalService>>());
    }

    public void Dispose()
    {
        _platformDb.Dispose();
        _platformConn.Dispose();
        _tenantDb.Dispose();
        _tenantConn.Dispose();
    }

    // ── GetProfileAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfileAsync_ExistingCustomer_ReturnsProfile()
    {
        var result = await _sut.GetProfileAsync(PlatformCustomerId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(PlatformCustomerId);
        result.Value.DisplayName.Should().Be("Test Customer");
    }

    [Fact]
    public async Task GetProfileAsync_UnknownCustomer_ReturnsFailure()
    {
        var result = await _sut.GetProfileAsync(999L);

        result.IsSuccess.Should().BeFalse();
    }

    // ── UpdateProfileAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfileAsync_ValidName_UpdatesDisplayName()
    {
        var result = await _sut.UpdateProfileAsync(PlatformCustomerId, new UpdateCustomerProfileDto("Updated Name", null));

        result.IsSuccess.Should().BeTrue();
        var profile = await _sut.GetProfileAsync(PlatformCustomerId);
        profile.Value!.DisplayName.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateProfileAsync_UnknownCustomer_ReturnsFailure()
    {
        var result = await _sut.UpdateProfileAsync(999L, new UpdateCustomerProfileDto("X", null));

        result.IsSuccess.Should().BeFalse();
    }

    // ── ListPurchasesAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ListPurchasesAsync_NoLinks_ReturnsEmptyPage()
    {
        var result = await _sut.ListPurchasesAsync(PlatformCustomerId, 1, 20);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    // ── ListLinkedShopsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ListLinkedShopsAsync_NoLinks_ReturnsEmptyPage()
    {
        var result = await _sut.ListLinkedShopsAsync(PlatformCustomerId, 1, 20);

        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ListLinkedShopsAsync_WithLinks_ReturnsLinkedShops()
    {
        _platformDb.CustomerLinks.Add(new CustomerLink
        {
            PlatformCustomerId = PlatformCustomerId,
            ShopId = ShopId,
            TenantCustomerId = 1L,
            IsActive = true,
            LinkedAtUtc = DateTime.UtcNow,
        });
        await _platformDb.SaveChangesAsync();

        var result = await _sut.ListLinkedShopsAsync(PlatformCustomerId, 1, 20);

        result.TotalCount.Should().Be(1);
        result.Items.First().ShopId.Should().Be(ShopId);
    }

    // ── GetInsightsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetInsightsAsync_NoLinks_ReturnsZeroInsights()
    {
        var result = await _sut.GetInsightsAsync(PlatformCustomerId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalSpend.Should().Be(0m);
        result.Value.TotalInvoices.Should().Be(0);
    }

    private void SeedCustomer()
    {
        _platformDb.PlatformCustomers.Add(new PlatformCustomer
        {
            Id = PlatformCustomerId,
            DisplayName = "Test Customer",
            Phone = "+919999999999",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        });
        _platformDb.SaveChanges();
    }
}

// ── Helpers ───────────────────────────────────────────────────────────────────

file sealed class StubTenantContext(long shopId) : ITenantContext
{
    public long ShopId => shopId;
    public long CurrentUserId => 0L;
    public IReadOnlyList<string> CurrentUserRoles => [];
}

// ── Minimal DbContext subclasses for unit tests ───────────────────────────────

file sealed class PortalPlatformDbContext(DbContextOptions<PlatformDbContext> opts, AuditSaveChangesInterceptor audit)
    : PlatformDbContext(opts, audit);

file sealed class PortalTenantDbContext(
    DbContextOptions<TenantDbContext> opts,
    ITenantContext ctx,
    AuditSaveChangesInterceptor audit,
    TenantSaveChangesInterceptor tenant,
    IEnumerable<IEntityModelConfigurator> configurators)
    : TenantDbContext(opts, ctx, audit, tenant, configurators);
