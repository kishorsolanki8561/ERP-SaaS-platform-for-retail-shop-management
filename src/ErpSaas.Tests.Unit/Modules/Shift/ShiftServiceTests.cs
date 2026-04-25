using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Messaging;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Shift.Entities;
using ErpSaas.Modules.Shift.Enums;
using ErpSaas.Modules.Shift.Infrastructure;
using ErpSaas.Modules.Shift.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ShiftEntity = ErpSaas.Modules.Shift.Entities.Shift;

namespace ErpSaas.Tests.Unit.Modules.Shift;

// ── Test-local DbContext that includes Shift entity config ────────────────────

/// <summary>
/// Extends <see cref="TenantDbContext"/> so that the SQLite in-memory provider
/// can resolve Shift entities during unit tests without modifying the
/// production <c>TenantDbContext</c>.
/// </summary>
internal sealed class ShiftTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        new ShiftModelConfigurator().Configure(modelBuilder);
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

[Trait("Category", "Unit")]
public class ShiftServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly ShiftService _sut;
    private readonly SqliteConnection _sqliteConnection;

    private const long ShopId = 1L;
    private const long BranchId = 10L;
    private const long CashierUserId = 99L;
    private const string CashierName = "John Doe";

    public ShiftServiceTests()
    {
        var stubCtx = new StubTenantContext(shopId: ShopId, userId: CashierUserId);

        // Use SQLite in-memory (supports transactions, unlike EF in-memory provider).
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;
        var auditInterceptor = new AuditSaveChangesInterceptor(stubCtx);
        var tenantInterceptor = new TenantSaveChangesInterceptor(stubCtx);

        _db = new ShiftTenantDbContext(opts, stubCtx, auditInterceptor, tenantInterceptor);
        _db.Database.EnsureCreated();

        _sut = new ShiftService(
            _db,
            _errorLogger,
            stubCtx,
            _notifications,
            Substitute.For<ILogger<ShiftService>>());
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqliteConnection.Dispose();
    }

    // ── OpenShiftAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task OpenShiftAsync_ValidDto_ReturnsSuccessWithId()
    {
        var dto = MakeOpenDto(openingCash: 500m);

        var result = await _sut.OpenShiftAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task OpenShiftAsync_CreatesShiftWithOpenStatus()
    {
        var dto = MakeOpenDto(openingCash: 1000m);

        var result = await _sut.OpenShiftAsync(dto);

        result.IsSuccess.Should().BeTrue();
        var shift = await _db.Set<ShiftEntity>().FindAsync(result.Value);
        shift.Should().NotBeNull();
        shift!.Status.Should().Be(ShiftStatus.Open);
        shift.OpeningCash.Should().Be(1000m);
        shift.BranchId.Should().Be(BranchId);
        shift.CashierNameSnapshot.Should().Be(CashierName);
    }

    [Fact]
    public async Task OpenShiftAsync_AlreadyOpenShift_ReturnsConflict()
    {
        await OpenShiftAndGetIdAsync();

        // Attempt to open a second shift for the same cashier + branch
        var result = await _sut.OpenShiftAsync(MakeOpenDto());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task OpenShiftAsync_WithDenominations_SavesDenominationCounts()
    {
        var denominations = new List<DenominationDto>
        {
            new(500, 2),
            new(100, 5),
        };
        var dto = new OpenShiftDto(BranchId, 1500m, denominations, CashierName);

        var result = await _sut.OpenShiftAsync(dto);

        result.IsSuccess.Should().BeTrue();
        var denoms = await _db.Set<ShiftDenominationCount>()
            .Where(d => d.ShiftId == result.Value)
            .ToListAsync();
        denoms.Should().HaveCount(2);
    }

    // ── CloseShiftAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CloseShiftAsync_OpenShift_ReturnsSuccess()
    {
        var shiftId = await OpenShiftAndGetIdAsync();
        var dto = new CloseShiftDto(ClosingCashCounted: 500m, Denominations: null, Notes: null);

        var result = await _sut.CloseShiftAsync(shiftId, dto);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CloseShiftAsync_ChangesStatusToClosed()
    {
        var shiftId = await OpenShiftAndGetIdAsync(openingCash: 500m);
        var dto = new CloseShiftDto(ClosingCashCounted: 490m, Denominations: null, Notes: "Day end");

        await _sut.CloseShiftAsync(shiftId, dto);

        var shift = await _db.Set<ShiftEntity>().FindAsync(shiftId);
        shift!.Status.Should().Be(ShiftStatus.Closed);
        shift.ClosingCashCounted.Should().Be(490m);
        shift.ClosingNotes.Should().Be("Day end");
    }

    [Fact]
    public async Task CloseShiftAsync_CalculatesCashVariance()
    {
        // OpeningCash = 500, TotalCashSales = 0, no movements → SystemComputedCash = 500
        // ClosingCounted = 490 → Variance = -10
        var shiftId = await OpenShiftAndGetIdAsync(openingCash: 500m);
        var dto = new CloseShiftDto(ClosingCashCounted: 490m, Denominations: null, Notes: null);

        await _sut.CloseShiftAsync(shiftId, dto);

        var shift = await _db.Set<ShiftEntity>().FindAsync(shiftId);
        shift!.SystemComputedCash.Should().Be(500m);
        shift.CashVariance.Should().Be(-10m);
    }

    [Fact]
    public async Task CloseShiftAsync_AlreadyClosedShift_ReturnsConflict()
    {
        var shiftId = await OpenShiftAndGetIdAsync();
        var dto = new CloseShiftDto(500m, null, null);
        await _sut.CloseShiftAsync(shiftId, dto);

        var result = await _sut.CloseShiftAsync(shiftId, dto);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CloseShiftAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.CloseShiftAsync(9999L, new CloseShiftDto(0m, null, null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // ── ForceCloseAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task ForceCloseAsync_OpenShift_ReturnsSuccess()
    {
        var shiftId = await OpenShiftAndGetIdAsync();

        var result = await _sut.ForceCloseAsync(shiftId, "Manager override");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ForceCloseAsync_ChangesStatusToForcedClosed()
    {
        var shiftId = await OpenShiftAndGetIdAsync();

        await _sut.ForceCloseAsync(shiftId, "System timeout");

        var shift = await _db.Set<ShiftEntity>().FindAsync(shiftId);
        shift!.Status.Should().Be(ShiftStatus.ForcedClosed);
        shift.ClosingNotes.Should().Be("System timeout");
        shift.ForcedClosedByUserId.Should().Be(CashierUserId);
    }

    [Fact]
    public async Task ForceCloseAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.ForceCloseAsync(9999L, "reason");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ForceCloseAsync_AlreadyClosedShift_ReturnsConflict()
    {
        var shiftId = await OpenShiftAndGetIdAsync();
        await _sut.CloseShiftAsync(shiftId, new CloseShiftDto(500m, null, null));

        var result = await _sut.ForceCloseAsync(shiftId, "too late");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    // ── IsShiftOpenAsync (IShiftLookup) ───────────────────────────────────────

    [Fact]
    public async Task IsShiftOpenAsync_OpenShift_ReturnsTrue()
    {
        var shiftId = await OpenShiftAndGetIdAsync();

        var result = await _sut.IsShiftOpenAsync(shiftId, ShopId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsShiftOpenAsync_ClosedShift_ReturnsFalse()
    {
        var shiftId = await OpenShiftAndGetIdAsync();
        await _sut.CloseShiftAsync(shiftId, new CloseShiftDto(500m, null, null));

        var result = await _sut.IsShiftOpenAsync(shiftId, ShopId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsShiftOpenAsync_NonExistingShift_ReturnsFalse()
    {
        var result = await _sut.IsShiftOpenAsync(9999L, ShopId);

        result.Should().BeFalse();
    }

    // ── GetShiftSummaryAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetShiftSummaryAsync_ExistingShift_ReturnsSummary()
    {
        var shiftId = await OpenShiftAndGetIdAsync(openingCash: 300m);

        var summary = await _sut.GetShiftSummaryAsync(shiftId);

        summary.Should().NotBeNull();
        summary!.Id.Should().Be(shiftId);
        summary.OpeningCash.Should().Be(300m);
        summary.Status.Should().Be(ShiftStatus.Open);
    }

    [Fact]
    public async Task GetShiftSummaryAsync_NonExistingShift_ReturnsNull()
    {
        var summary = await _sut.GetShiftSummaryAsync(9999L);

        summary.Should().BeNull();
    }

    // ── ListShiftsAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListShiftsAsync_ReturnsAllShiftsForShop()
    {
        await OpenShiftAndGetIdAsync();

        var result = await _sut.ListShiftsAsync(1, 50, null);

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task ListShiftsAsync_FiltersByBranchId()
    {
        await OpenShiftAndGetIdAsync(); // BranchId = 10
        // No way to open a second shift for same cashier+branch (conflict guard)
        // So we only have 1 shift; filter by a different branch returns 0
        var result = await _sut.ListShiftsAsync(1, 50, 999L);

        result.Items.Should().BeEmpty();
    }

    // ── RecordCashInAsync / RecordCashOutAsync ────────────────────────────────

    [Fact]
    public async Task RecordCashInAsync_OpenShift_ReturnsSuccess()
    {
        var shiftId = await OpenShiftAndGetIdAsync();
        var dto = new CashMovementDto(Amount: 200m, ReasonCode: null, Notes: "Float top-up");

        var result = await _sut.RecordCashInAsync(shiftId, dto);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RecordCashOutAsync_OpenShift_ReturnsSuccess()
    {
        var shiftId = await OpenShiftAndGetIdAsync();
        var dto = new CashMovementDto(Amount: 100m, ReasonCode: "PETTY", Notes: null);

        var result = await _sut.RecordCashOutAsync(shiftId, dto);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RecordCashInAsync_ClosedShift_ReturnsConflict()
    {
        var shiftId = await OpenShiftAndGetIdAsync();
        await _sut.CloseShiftAsync(shiftId, new CloseShiftDto(500m, null, null));

        var result = await _sut.RecordCashInAsync(shiftId, new CashMovementDto(50m, null, null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static OpenShiftDto MakeOpenDto(decimal openingCash = 500m)
        => new(BranchId, openingCash, null, CashierName);

    private async Task<long> OpenShiftAndGetIdAsync(decimal openingCash = 500m)
    {
        var result = await _sut.OpenShiftAsync(MakeOpenDto(openingCash));
        result.IsSuccess.Should().BeTrue("pre-condition: shift must open successfully");
        return result.Value!;
    }

    private sealed class StubTenantContext(long shopId, long userId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => userId;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
